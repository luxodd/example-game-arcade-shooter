using System;
using Luxodd.Game.Scripts.Network.CommandHandler;
using Luxodd.Game.Scripts.Network.Payloads;
using UnityEngine;

namespace Luxodd.Game.Scripts.Network
{
    /// <summary>Opt-in reporting of absolute game-owned score snapshots.</summary>
    public sealed class ScoreUpdateService : MonoBehaviour
    {
        [SerializeField] private WebSocketCommandHandler _commandHandler;
        [SerializeField, Min(0.01f)] private float _intervalSeconds = 2f;
        private const float ImmediateTimeoutSeconds = 2f;
        private int _level;
        private int _score;
        private long _nextRequestId = 1;
        private int _lifecycle;
        private float _nextTick;
        private Request _pending;
        private ImmediateOperation _immediate;
        private WebSocketService _subscribedSocket;
        private Func<float> _timeProvider = () => Time.realtimeSinceStartup;

        public bool IsTracking { get; private set; }
        public event Action<ScoreUpdatePayload> ScoreUpdateSent;
        public event Action<ScoreUpdateResponse> ScoreUpdateHandled;
        public event Action<int, string> ScoreUpdateFailed;

        private sealed class Request
        {
            public ScoreUpdatePayload Snapshot;
            public int Lifecycle;
        }

        private sealed class ImmediateOperation
        {
            public int Level;
            public int Score;
            public float Deadline;
            public Request Request;
            public Action<ScoreUpdateResponse> Success;
            public Action<int, string> Failure;
        }

        public void StartTracking(int level, int initialScore)
        {
            if (IsTracking)
            {
                Debug.LogWarning("[ScoreUpdate] StartTracking rejected: tracking is already active.");
                return;
            }
            if (!IsValid(level, initialScore)) return;
            SubscribeToSession();
            if (_subscribedSocket == null || _subscribedSocket.IsSessionReplacementInProgress)
            {
                Debug.LogWarning("[ScoreUpdate] StartTracking requires an assigned command handler and no session handoff.");
                return;
            }
            _lifecycle++;
            _level = level;
            _score = initialScore;
            _nextTick = _timeProvider() + Mathf.Max(0.01f, _intervalSeconds);
            IsTracking = true;
        }

        public void UpdateCurrentState(int level, int score)
        {
            if (!IsTracking || !IsValid(level, score)) return;
            _level = level;
            _score = score;
        }

        public void SendCurrentScoreNow(Action<ScoreUpdateResponse> onSuccessCallback = null,
            Action<int, string> onFailureCallback = null)
        {
            if (_immediate != null)
            {
                onFailureCallback?.Invoke(409, "An immediate score update is already active.");
                return;
            }
            if (!IsTracking)
            {
                onFailureCallback?.Invoke(400, "Score tracking is inactive.");
                return;
            }
            _immediate = new ImmediateOperation
            {
                Level = _level, Score = _score,
                Deadline = _timeProvider() + ImmediateTimeoutSeconds,
                Success = onSuccessCallback, Failure = onFailureCallback
            };
            if (_pending == null) SendSnapshot(_immediate.Level, _immediate.Score, true);
        }

        public void StopTracking()
        {
            IsTracking = false;
            _lifecycle++;
            var pending = _pending;
            var immediate = _immediate;
            _pending = null;
            _immediate = null;
            if (pending != null)
                _subscribedSocket?.CancelScoreUpdate(pending.Snapshot.RequestId, -1, "Score tracking stopped.");
            immediate?.Failure?.Invoke(-1, "Score tracking stopped.");
        }

        private void OnEnable() => SubscribeToSession();
        private void OnDisable() => StopTracking();
        private void OnDestroy()
        {
            StopTracking();
            if (_subscribedSocket != null) _subscribedSocket.SessionInvalidated -= OnSessionInvalidated;
            _subscribedSocket = null;
        }
        private void Update() => ProcessSchedule(_timeProvider());

        private void SubscribeToSession()
        {
            var socket = _commandHandler == null ? null : _commandHandler.SocketService;
            if (_subscribedSocket == socket) return;
            if (_subscribedSocket != null) _subscribedSocket.SessionInvalidated -= OnSessionInvalidated;
            _subscribedSocket = socket;
            if (socket != null) socket.SessionInvalidated += OnSessionInvalidated;
        }

        private void OnSessionInvalidated()
        {
            _nextRequestId = 1;
            _level = 0;
            _score = 0;
            StopTracking();
        }

        private static bool IsValid(int level, int score)
        {
            if (level >= 0 && score >= 0) return true;
            Debug.LogWarning("[ScoreUpdate] Level and score must be non-negative.");
            return false;
        }

        private void ProcessSchedule(float now)
        {
            if (!IsTracking) return;
            if (_immediate != null && now >= _immediate.Deadline)
            {
                var immediate = _immediate;
                _immediate = null;
                if (immediate.Request != null && ReferenceEquals(_pending, immediate.Request))
                {
                    _pending = null;
                    _subscribedSocket.CancelScoreUpdate(immediate.Request.Snapshot.RequestId, 408,
                        "Immediate score update timed out.");
                }
                immediate.Failure?.Invoke(408, "Immediate score update exceeded its two-second deadline.");
            }
            if (!IsTracking || now < _nextTick) return;
            // Advance from now: missed ticks are discarded, never replayed.
            _nextTick = now + Mathf.Max(0.01f, _intervalSeconds);
            if (_pending == null && _immediate == null && _subscribedSocket.IsConnected)
                SendSnapshot(_level, _score, false);
        }

        private void SendSnapshot(int level, int score, bool immediate)
        {
            if (_nextRequestId > int.MaxValue)
            {
                CompleteRequest(null, null, 400, "Score update request IDs exhausted.");
                return;
            }
            var request = new Request
            {
                Lifecycle = _lifecycle,
                Snapshot = new ScoreUpdatePayload { RequestId = (int)_nextRequestId++, Level = level, Score = score }
            };
            _pending = request;
            if (immediate) _immediate.Request = request;
            ScoreUpdateSent?.Invoke(new ScoreUpdatePayload
            {
                RequestId = request.Snapshot.RequestId, Level = level, Score = score
            });
            if (!IsTracking || !ReferenceEquals(_pending, request)) return;
            _commandHandler.SendScoreUpdateRequestCommand(request.Snapshot.RequestId, level, score,
                response => CompleteRequest(request, response, 200, null),
                (status, reason) => CompleteRequest(request, null, status, reason));
        }

        private void CompleteRequest(Request request, ScoreUpdateResponse response, int status, string reason)
        {
            if (request != null && (!ReferenceEquals(_pending, request) || request.Lifecycle != _lifecycle)) return;
            _pending = null;
            var lifecycle = _lifecycle;
            var immediate = _immediate != null && ReferenceEquals(_immediate.Request, request) ? _immediate : null;
            if (immediate != null) _immediate = null;
            // An ACK processed after the death-boundary deadline cannot extend the caller's wait.
            var immediateExpired = immediate != null && _timeProvider() >= immediate.Deadline;
            if (response != null)
            {
                switch (response.Result)
                {
                    case "applied":
                    case "duplicate":
                        break;
                    case "stale":
                        Debug.LogWarning($"[ScoreUpdate] Stale snapshot handled: request_id={response.RequestId}.");
                        break;
                    case "finalized":
                    case "session_ended":
                        StopTracking();
                        break;
                    default:
                        Debug.LogWarning($"[ScoreUpdate] Handled status=200 with result={response.Result}.");
                        break;
                }
                ScoreUpdateHandled?.Invoke(response);
                if (immediateExpired)
                    immediate.Failure?.Invoke(408, "Immediate score update exceeded its two-second deadline.");
                else
                    immediate?.Success?.Invoke(response);
            }
            else
            {
                ScoreUpdateFailed?.Invoke(status, reason);
                immediate?.Failure?.Invoke(status, reason);
            }
            if (!IsTracking || lifecycle != _lifecycle || _pending != null || _immediate == null) return;
            if (_timeProvider() >= _immediate.Deadline)
            {
                var expired = _immediate;
                _immediate = null;
                expired.Failure?.Invoke(408, "Immediate score update exceeded its two-second deadline.");
                return;
            }
            SendSnapshot(_immediate.Level, _immediate.Score, true);
        }
    }
}

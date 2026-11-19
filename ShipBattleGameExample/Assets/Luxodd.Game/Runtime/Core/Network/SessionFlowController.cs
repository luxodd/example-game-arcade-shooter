using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Luxodd.Game.Scripts.Network
{
    public enum SessionFlowResult
    {
        EventPath,
        LegacyPath,
        Error
    }
    
    public class SessionFlowController : MonoBehaviour
    {
        public bool SessionPayloadIsPresent => _sessionPayload != null;
        
        [SerializeField] private float _waitTimeoutSeconds = 5f;
        [SerializeField] private LuxoddSessionBridge _sessionBridge;
        [SerializeField] private WebSocketService _socketService;
        
        private LuxoddSessionPayload _sessionPayload;
        
        private readonly Queue<StartupRegistration> _startupRegistrations = new Queue<StartupRegistration>();
        
        private Coroutine _coroutine;
        private bool _initialConnectionStarted;
        private bool _initialConnectionCompleted;
        private bool _initialConnectionFailed;
        private bool _initialSessionRetired;
        private bool _isReplacingInitialSession;
        private bool _isDeliveringCallbacks;
        private bool _reconcileOnUpdate;
        private bool _isDestroyed;
        private string _initialSessionToken;

        internal bool InitialConnectionStartedForFastRestartTrace => _initialConnectionStarted;
        internal WebSocketService SocketService => _socketService;

        private void Awake()
        {
            _sessionBridge.BindSessionFlowController(this);
            _sessionBridge.OnSessionReceived.AddListener(OnSessionReceivedHandler);
            _socketService.ConnectedToServerEvent.AddListener(OnConnectionStateChanged);
            _socketService.SessionInvalidated += OnSessionInvalidated;

            // This payload already passed the bridge's duplicate and stale-session checks.
            var acceptedPayload = _sessionBridge.SessionPayload;
            if (acceptedPayload != null && acceptedPayload.IsValid)
            {
                _sessionPayload = acceptedPayload;
            }
        }

        private void OnDisable()
        {
            CancelPendingRegistrations();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            CancelPendingRegistrations();

            if (_sessionBridge != null)
            {
                _sessionBridge.OnSessionReceived.RemoveListener(OnSessionReceivedHandler);
                _sessionBridge.UnbindSessionFlowController(this);
            }

            if (_socketService != null)
            {
                _socketService.ConnectedToServerEvent.RemoveListener(OnConnectionStateChanged);
                _socketService.SessionInvalidated -= OnSessionInvalidated;
            }
        }

        private void Update()
        {
            // Continue/cancel may release a Fast Restart popup without changing socket state.
            if (_reconcileOnUpdate || _startupRegistrations.Count > 0)
            {
                _reconcileOnUpdate = false;
                RecordInitialConnectionCompletion();
                DeliverStartupOutcomes();
            }
        }
        
        /// <summary>
        /// Registers one startup waiter without restarting the initial connection. Later registrations
        /// also complete once: they wait through recovery, or fail if the initial session was retired.
        /// Disabling or destroying the controller fails pending registrations without closing transport.
        /// </summary>
        public void ActivateProcess(System.Action onConnectedToServerCallback, System.Action onConnectToServerErrorCallback)
        {
            var registration = new StartupRegistration(onConnectedToServerCallback, onConnectToServerErrorCallback);
            registration.IsCancelled = _isDestroyed || isActiveAndEnabled == false;
            _startupRegistrations.Enqueue(registration);
            if (registration.IsCancelled)
            {
                DeliverStartupOutcomes();
                return;
            }

            if (_initialConnectionStarted == false && _initialConnectionFailed == false &&
                _initialSessionRetired == false && _coroutine == null)
            {
                if (_sessionPayload != null || _sessionBridge.HasSession ||
                    _socketService.IsConnected || _socketService.IsConnectionAttemptInProgress)
                {
                    CheckPayloadAndConnectToServer();
                }
                else
                {
                    _coroutine = StartCoroutine(StartListenEventProcess());
                }
            }

            // Activation can itself come from another connection-state listener. Always defer replay
            // so listener ordering cannot enter gameplay while the service is completing a transport.
            _reconcileOnUpdate = true;
        }

        private void OnSessionReceivedHandler(LuxoddSessionPayload payload)
        {
            if (_isDestroyed || payload == null || payload.IsValid == false)
            {
                return;
            }

            // Recovery may have opened A before Update (or before this listener receives the event).
            // Record that history before deciding whether the incoming session can correct startup.
            RecordInitialConnectionCompletion();
            _socketService.TraceFastRestartSessionDecision(payload, this, _initialConnectionStarted,
                _sessionPayload != null && _sessionPayload.Matches(payload));

            if (_initialConnectionStarted && _sessionPayload != null && _sessionPayload.Matches(payload))
            {
                return;
            }

            Action onConnectedCallback = _initialConnectionCompleted ? null : OnInitialConnectionSucceeded;
            Action<int, string> onFailureCallback = _initialConnectionCompleted ? null : OnSessionReplacementFailed;
            // Fast Restart owns early replacement delivery regardless of who connected Session A.
            if (_socketService.TryBufferFastRestartReplacementSession(payload,
                    () => AcceptSessionPayload(payload), onConnectedCallback, onFailureCallback))
            {
                _sessionBridge.MarkSessionPayloadBuffered(payload);
                return;
            }

            if (_initialConnectionStarted || _socketService.IsSessionReplacementInProgress)
            {
                AcceptSessionPayload(payload);
                // A late host token may correct legacy startup before its first successful connection.
                // Fast Restart and later replacements still retire the initial startup owner.
                _isReplacingInitialSession = _initialConnectionCompleted == false &&
                    _initialConnectionFailed == false && _initialSessionRetired == false &&
                    _socketService.IsFastRestartInProgress == false &&
                    _socketService.IsSessionARecoveryBlockedAfterFastRestart == false;
                if (_isReplacingInitialSession)
                {
                    _initialSessionToken = payload.Token;
                }

                try
                {
                    _socketService.ReplaceSession(payload, onConnectedCallback, onFailureCallback);
                }
                finally
                {
                    _isReplacingInitialSession = false;
                }
                return;
            }

            AcceptSessionPayload(payload);
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            
            CheckPayloadAndConnectToServer();
        }

        private void AcceptSessionPayload(LuxoddSessionPayload payload)
        {
            _sessionPayload = payload;
            _sessionBridge.AcceptSessionPayload(payload);
        }

        private void GoLegacyPath()
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(GoLegacyPath)}] OK");
            _initialConnectionStarted = true;
            _socketService.ConnectToServer(OnInitialConnectionSucceeded, OnInitialConnectionFailed);
        }

        private void GoEventPath()
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(GoEventPath)}] OK");
            _initialConnectionStarted = true;
            _socketService.ConnectToServer(_sessionPayload, OnInitialConnectionSucceeded, OnInitialConnectionFailed);
        }

        private void OnInitialConnectionSucceeded()
        {
            if (_isDestroyed || _initialConnectionFailed || _initialSessionRetired)
            {
                return;
            }

            RecordInitialConnectionCompletion();
            DeliverStartupOutcomes();
        }

        private void OnInitialConnectionFailed()
        {
            if (_isDestroyed) return;
            _initialConnectionFailed = true;
            _reconcileOnUpdate = true;
        }

        private void OnSessionReplacementFailed(int statusCode, string reason)
        {
            OnInitialConnectionFailed();
        }
        
        private IEnumerator StartListenEventProcess()
        {
            var timeCounter = 0f;

            while (timeCounter < _waitTimeoutSeconds && _sessionBridge.HasSession == false)
            {
                timeCounter += Time.deltaTime;
                yield return null;
            }
            
            CheckPayloadAndConnectToServer();
        }

        private void CheckPayloadAndConnectToServer()
        {
            _coroutine = null;
            if (_isDestroyed || isActiveAndEnabled == false || _initialConnectionStarted ||
                _initialConnectionFailed || _initialSessionRetired)
            {
                return;
            }

            if (SessionHandoffOwnsConnection())
            {
                OnInitialConnectionFailed();
                return;
            }

            _initialConnectionStarted = true;
            _initialSessionToken = _sessionPayload != null ? _sessionPayload.Token : _socketService.SessionToken;
            if (_socketService.IsConnected || _socketService.IsConnectionAttemptInProgress)
            {
                // Join an existing initial attempt without replacing its transport or callbacks.
                if (string.Equals(_initialSessionToken, _socketService.SessionToken, StringComparison.Ordinal) == false)
                {
                    OnInitialConnectionFailed();
                }
                else if (_socketService.IsConnected)
                {
                    RecordInitialConnectionCompletion();
                    _reconcileOnUpdate = true;
                }
                return;
            }

            if (_sessionPayload == null)
            {
                GoLegacyPath();
            }
            else
            {
                GoEventPath();
            }
        }

        private void OnConnectionStateChanged(bool isConnected)
        {
            if (_isDestroyed) return;
            // Recording history is safe inside the event; gameplay delivery still waits for Update.
            if (isConnected) RecordInitialConnectionCompletion();
            // The service has not finished processing its open/close callback yet.
            // Recheck usability and ownership in Update, including internal recovery.
            _reconcileOnUpdate = true;
        }

        private void OnSessionInvalidated()
        {
            if (_initialConnectionStarted && _isReplacingInitialSession == false)
            {
                _initialSessionRetired = true;
                _reconcileOnUpdate = true;
            }
        }

        private bool SessionHandoffOwnsConnection()
        {
            return _socketService.IsSessionReplacementInProgress || _socketService.IsFastRestartInProgress ||
                   _socketService.IsSessionARecoveryBlockedAfterFastRestart;
        }

        private void RecordInitialConnectionCompletion()
        {
            if (_isDestroyed == false && _initialConnectionStarted && _initialConnectionFailed == false &&
                _initialSessionRetired == false && _socketService.IsConnected &&
                _socketService.IsConnectionAttemptInProgress == false && SessionHandoffOwnsConnection() == false &&
                string.Equals(_initialSessionToken, _socketService.SessionToken, StringComparison.Ordinal))
            {
                _initialConnectionCompleted = true;
            }
        }

        private void CancelPendingRegistrations()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            // Mark the entire batch before invoking callbacks: one may re-enable this controller.
            foreach (var registration in _startupRegistrations)
                registration.IsCancelled = true;
            DeliverStartupOutcomes();
        }

        private void DeliverStartupOutcomes()
        {
            if (_isDeliveringCallbacks)
            {
                _reconcileOnUpdate = true;
                return;
            }

            _isDeliveringCallbacks = true;
            try
            {
                // Registrations made by a callback belong to the next reconciliation boundary.
                var registrationCount = _startupRegistrations.Count;
                while (_startupRegistrations.Count > 0)
                {
                    var registration = _startupRegistrations.Peek();
                    // Cancelled reentrant callers must also terminate when Update cannot run.
                    if (registrationCount-- <= 0 && registration.IsCancelled == false) return;
                    var failed = registration.IsCancelled || _isDestroyed || _initialConnectionFailed ||
                                 _initialSessionRetired;
                    if (failed == false)
                    {
                        if (isActiveAndEnabled == false || _initialConnectionStarted == false ||
                            _socketService.IsConnected == false ||
                            _socketService.IsConnectionAttemptInProgress || SessionHandoffOwnsConnection())
                        {
                            return;
                        }

                        if (string.Equals(_initialSessionToken, _socketService.SessionToken,
                                StringComparison.Ordinal) == false)
                        {
                            _initialSessionRetired = true;
                            failed = true;
                        }
                    }

                    // Removing the registration commits its outcome before entering game code.
                    _startupRegistrations.Dequeue();
                    try
                    {
                        if (failed)
                        {
                            registration.OnFailure?.Invoke();
                        }
                        else
                        {
                            registration.OnSuccess?.Invoke();
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                    }
                }
            }
            finally
            {
                _isDeliveringCallbacks = false;
            }
        }

        private sealed class StartupRegistration
        {
            internal readonly Action OnSuccess;
            internal readonly Action OnFailure;
            internal bool IsCancelled;

            internal StartupRegistration(Action onSuccess, Action onFailure)
            {
                OnSuccess = onSuccess;
                OnFailure = onFailure;
            }
        }
    }
}

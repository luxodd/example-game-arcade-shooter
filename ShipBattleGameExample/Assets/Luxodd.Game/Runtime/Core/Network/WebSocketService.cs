
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Network.CommandHandler;

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Luxodd.Game.Scripts.Network
{
    public class WebSocketService : MonoBehaviour
    {
        private const float CommandResponseTimeoutSeconds = 10f;
        private const int CommandResponseTimeoutStatusCode = 408;
        private const int TransportFailureStatusCode = 1006;
        private const int FastRestartAlreadyPendingStatusCode = 409;
        private const int FastRestartInvalidSessionStatusCode = 400;
        private const float FastRestartHandoffTimeoutSeconds = 10f;
        private const float SessionReplacementConnectionTimeoutSeconds = 10f;
        internal const int SessionReplacementStatusCode = -1;

        public bool IsConnected => _isConnected;
        public string SessionToken => GetSessionToken();
        public ISimpleEvent<bool> ConnectedToServerEvent =>  _isConnectedEvent;
        
        
        [SerializeField] private NetworkSettingsDescriptor _settingsDescriptor = null;
        [SerializeField] private WebSocketLibraryWrapper _socketLibraryWrapper = null;

        [SerializeField] private FetchUrlQueryString _fetchUrlQueryString = null;
        [SerializeField] private WebGlHostWrapper _webGlHostWrapper = null;

        [SerializeField] private float _secondsBeforeError = 4f;

        //for test purpose
        [Header("For Test")] [SerializeField] private float _timeBeforeReconnect = 3f;

        private ClientWebSocket _clientWebSocket;
        private bool _isConnected;
        private bool _isInReconnection = false;
        private bool _isConnectionAttemptInProgress;
        private bool _currentAttemptIsInternalRecovery;
        private bool _isInFlushing = false;
        private bool _isResettingTransport;
        private bool _isApplicationQuitting;
        private bool _transportCloseRequested;
        private bool _isSessionReplacementInProgress;
        private bool _sessionTransportInvalidated;
        private bool _isFastRestartInProgress;
        private bool _combinedSessionOptionOwnsActions;
        private bool _isSessionARecoveryBlockedAfterFastRestart;
        private bool _fastRestartRestartSelected;
        private bool _fastRestartReplacementSessionReceived;
        private bool _fastRestartReplacementConnected;
        private float _fastRestartHandoffDeadline;
        private float _sessionReplacementConnectionDeadline;
        private BufferedFastRestartReplacementSession _bufferedFastRestartReplacementSession;
#if UNITY_EDITOR
        private bool _editorReconnectScheduled;
#endif
        private int _transportGeneration;
        private int _connectionAttemptId;
        private Coroutine _waitForConnectionCoroutine;
        private Coroutine _flushCommandsCoroutine;

#if UNITY_EDITOR
        private Action<Uri> _connectionAttemptTestHook;
        private Func<string, Task<bool>> _sendMessageTestHook;
#endif

        private System.Action<string> _successCallback;
        private System.Action<string> _errorCallback;

        private Action _onConnectedCallback;
        private Action _onConnectionErrorCallback;

        private Action<SessionOptionAction> _onSessionOptionCallback;
        private Action _onSessionReplacementConnectedCallback;
        private Action<int, string> _onSessionReplacementErrorCallback;
        private Action<SessionOptionAction> _onFastRestartCallback;
        private Action<int, string> _onFastRestartFailureCallback;
        private Action<FastRestartPreparationContext> _onFastRestartPreparation;
        private FastRestartPreparationContext _restartPreparationContext;
        private readonly HashSet<string> _acceptedRestartPreparationRequests = new HashSet<string>(StringComparer.Ordinal);
        private int _restartPreparationPopupId;
        private int _restartPreparationTransportGeneration;
        private string _restartPreparationSessionToken;
        private bool _restartPreparationReady;
        private float _restartPreparationDeadline;
        
        private readonly SimpleEvent<bool> _isConnectedEvent = new SimpleEvent<bool>();

        private Dictionary<CommandRequestType, Queue<CommandRequestHandler>> _commandRequestHandlers =
            new Dictionary<CommandRequestType, Queue<CommandRequestHandler>>();

        private Queue<SendCommandData> _sendCommandDataQueue = new Queue<SendCommandData>();
        private LuxoddSessionPayload _sessionPayload;

        // Temporary staging diagnostics only; none of these fields control session ownership.
        // Instance trace values are managed-reference diagnostic hashes, not guaranteed unique Unity object IDs.
        private LuxoddSessionPayload _fastRestartTraceSessionA;
        private SessionFlowController _fastRestartTraceController;
        private string _fastRestartTraceTransportOwner = "Unknown";
        private int _fastRestartTraceTransportAttemptId;
        private int _fastRestartTraceTransportGeneration;

        internal void TraceFastRestartSessionDecision(LuxoddSessionPayload payload,
            SessionFlowController controller, bool initialConnectionStarted, bool matchesController)
        {
            _fastRestartTraceController = controller;
            var matchesCurrent = _sessionPayload != null && _sessionPayload.Matches(payload);
            var wouldBuffer = _isFastRestartInProgress && _fastRestartRestartSelected == false &&
                              payload != null && payload.IsValid && matchesCurrent == false;
            var decision = initialConnectionStarted && matchesController ? "DUPLICATE" :
                wouldBuffer ? "BUFFER" : initialConnectionStarted == false ? "INITIAL_ACCEPT" : "ACCEPT_REPLACEMENT";
            Debug.Log($"[FastRestartTrace] SessionDecision initialConnectionStarted={initialConnectionStarted} " +
                      $"fastRestartInProgress={_isFastRestartInProgress} restartSelected={_fastRestartRestartSelected} " +
                      $"replacementInProgress={_isSessionReplacementInProgress} connected={_isConnected} " +
                      $"matchesCurrent={matchesCurrent} matchesController={matchesController} " +
                      $"hasBufferedReplacement={_bufferedFastRestartReplacementSession != null} " +
                      $"controllerInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(controller)} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)} decision={decision}");
        }

        private void TraceFastRestartBufferCheck(LuxoddSessionPayload payload, bool buffered)
        {
            Debug.Log($"[FastRestartTrace] BufferCheck fastRestartInProgress={_isFastRestartInProgress} " +
                      $"restartSelected={_fastRestartRestartSelected} " +
                      $"matchesCurrent={_sessionPayload != null && _sessionPayload.Matches(payload)} " +
                      $"serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)} result={(buffered ? "BUFFERED" : "REJECTED")}");
            if (buffered)
            {
                Debug.Log($"[FastRestartTrace] ReplacementBuffered connectionStarted=False " +
                          $"existingAttemptInProgress={_isConnectionAttemptInProgress} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
            }
        }

        private string GetFastRestartTraceOwner(LuxoddSessionPayload payload)
        {
            if (_isFastRestartInProgress == false)
            {
                return _isSessionReplacementInProgress ? "Replacement" : "Current";
            }

            // Compare identities with the request-time snapshot, never interpret credential text.
            if (payload == null)
            {
                return _fastRestartTraceSessionA == null ? "A" : "Unknown";
            }

            return _fastRestartTraceSessionA != null && _fastRestartTraceSessionA.Matches(payload) ? "A" : "B";
        }

        private void TraceFastRestartTransportStart(LuxoddSessionPayload payload, int attemptId, int generation)
        {
            var isFastRestartRelated = _isFastRestartInProgress || _isSessionReplacementInProgress;
            _fastRestartTraceTransportOwner = isFastRestartRelated ? GetFastRestartTraceOwner(payload) : "Current";
            _fastRestartTraceTransportAttemptId = attemptId;
            _fastRestartTraceTransportGeneration = generation;
            if (isFastRestartRelated)
            {
                Debug.Log($"[FastRestartTrace] TransportConnectionStart owner={_fastRestartTraceTransportOwner} " +
                          $"attemptId={attemptId} transportGeneration={generation} " +
                          $"replacementInProgress={_isSessionReplacementInProgress} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
            }
        }

        internal bool IsConnectionAttemptInProgress => _isConnectionAttemptInProgress;
        internal bool IsSessionReplacementInProgress => _isSessionReplacementInProgress;
        internal bool IsFastRestartInProgress => _isFastRestartInProgress;
        internal bool IsSessionARecoveryBlockedAfterFastRestart => _isSessionARecoveryBlockedAfterFastRestart;

        // Evidence of the existing URL transport owner, never a session payload or transport identity.
        private string _initialUrlMetadataToken;
        private string _initialUrlMetadataEndpoint;
        private bool _urlMetadataOwnerRetired;

        internal bool TryGetInitialUrlMetadataOwner(out string token, out string endpoint)
        {
            token = null;
            endpoint = null;
            if (_urlMetadataOwnerRetired || _sessionPayload != null || _isFastRestartInProgress ||
                _isSessionReplacementInProgress || _isSessionARecoveryBlockedAfterFastRestart ||
                _sessionTransportInvalidated || _isApplicationQuitting || !isActiveAndEnabled ||
                string.IsNullOrWhiteSpace(_initialUrlMetadataToken) ||
                !string.Equals(SessionToken, _initialUrlMetadataToken, StringComparison.Ordinal))
                return false;

            token = _initialUrlMetadataToken;
            endpoint = _initialUrlMetadataEndpoint;
            return true;
        }

        private void ObserveUrlMetadataOwner(string token, string endpoint)
        {
            if (_initialUrlMetadataToken == null)
            {
                _initialUrlMetadataToken = token;
                _initialUrlMetadataEndpoint = endpoint;
            }
            else if (!string.Equals(token, _initialUrlMetadataToken, StringComparison.Ordinal) ||
                     !string.Equals(endpoint, _initialUrlMetadataEndpoint, StringComparison.Ordinal))
            {
                _urlMetadataOwnerRetired = true;
            }
        }

        internal event Action<bool> FastRestartRecoveryOwnershipResolved;

        // Gameplay session invalidation is independent of ordinary transport recovery.
        internal event Action SessionInvalidated;
        private CommandRequestHandler _scoreUpdateRequest;
        private const float ScoreUpdateTimeoutSeconds = 2f;

        internal async void SendScoreUpdateCommand(int requestId, string json,
            Action<CommandRequestHandler> onCompleted)
        {
            var request = new CommandRequestHandler
            {
                CommandRequestType = CommandRequestType.ScoreUpdateRequest,
                Id = requestId,
                TransportGeneration = _transportGeneration,
                OnCommandResponseSuccessHandler = onCompleted,
                ResponseDeadline = Time.realtimeSinceStartup + ScoreUpdateTimeoutSeconds
            };
            if (ShouldRejectCommandDuringSessionHandoff())
            {
                FailCommand(request, SessionReplacementStatusCode, "Score update rejected during session replacement.");
                return;
            }
            if (!_isConnected || _isResettingTransport || _isApplicationQuitting)
            {
                FailCommand(request, TransportFailureStatusCode, "Score update requires a connected socket.");
                return;
            }
            if (_scoreUpdateRequest != null)
            {
                FailCommand(request, 409, "A score update is already pending.");
                return;
            }

            _scoreUpdateRequest = request;
            var sent = await SendEventInner(json, request);
            if (request.IsCompleted || request.TransportGeneration != _transportGeneration) return;
            if (!sent)
            {
                // Actual socket failures retain the existing transport recovery behavior.
                ResetTransportGeneration(TransportFailureStatusCode, "WebSocket send failed.", true, false);
            }
        }

        internal void CancelScoreUpdate(int requestId, int statusCode, string reason)
        {
            var request = _scoreUpdateRequest;
            if (request == null || request.Id != requestId) return;
            _scoreUpdateRequest = null;
            FailCommand(request, statusCode, reason);
        }

        private void ProcessScoreUpdateTimeout(float now)
        {
            var request = _scoreUpdateRequest;
            if (request != null && request.ResponseDeadline <= now)
                CancelScoreUpdate(request.Id, CommandResponseTimeoutStatusCode, "Score update response timed out.");
        }

        public void ConnectToServer(Action onSuccessCallback = null, Action onErrorCallback = null)
        {
            if (ShouldStartOrdinaryConnection(onSuccessCallback, onErrorCallback) == false)
            {
                return;
            }

            _onConnectedCallback = onSuccessCallback;
            _onConnectionErrorCallback = onErrorCallback;
            TryStartConnectionAttempt(false);
        }

        internal void ConnectToServer(LuxoddSessionPayload sessionPayload, Action onSuccessCallback = null, Action onErrorCallback = null)
        {
            if (ShouldStartOrdinaryConnection(onSuccessCallback, onErrorCallback) == false)
            {
                return;
            }

            _sessionPayload = sessionPayload;
            _onConnectedCallback = onSuccessCallback;
            _onConnectionErrorCallback = onErrorCallback;
            TryStartConnectionAttempt(false);
        }

        internal void ConnectFromSessionBridge(LuxoddSessionPayload sessionPayload)
        {
            if (sessionPayload == null || sessionPayload.IsValid == false ||
                (_sessionPayload != null && _sessionPayload.Matches(sessionPayload) == false))
            {
                Debug.LogWarning("[WebSocketService] Bridge connection rejected: session credentials do not match.");
                return;
            }

            ConnectToServer(sessionPayload);
        }

        private bool ShouldStartOrdinaryConnection(Action onSuccessCallback, Action onErrorCallback)
        {
            // Ordinary callers cannot take ownership from a handoff or an accepted attempt.
            if (_isFastRestartInProgress || _isSessionReplacementInProgress ||
                _isSessionARecoveryBlockedAfterFastRestart || CanStartConnectionAttempt() == false)
            {
                onErrorCallback?.Invoke();
                return false;
            }

            if (_isConnected)
            {
                onSuccessCallback?.Invoke();
                return false;
            }

            return true;
        }

        internal bool TryBufferFastRestartReplacementSession(LuxoddSessionPayload sessionPayload,
            Action onSessionAcceptedCallback, Action onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (_isFastRestartInProgress == false || _fastRestartRestartSelected ||
                sessionPayload == null || sessionPayload.IsValid == false)
            {
                TraceFastRestartBufferCheck(sessionPayload, false);
                return false;
            }

            if (_sessionPayload != null && _sessionPayload.Matches(sessionPayload))
            {
                TraceFastRestartBufferCheck(sessionPayload, false);
                return false;
            }

            if (_bufferedFastRestartReplacementSession != null &&
                _bufferedFastRestartReplacementSession.SessionPayload.Matches(sessionPayload))
            {
                TraceFastRestartBufferCheck(sessionPayload, true);
                return true;
            }

            _bufferedFastRestartReplacementSession = new BufferedFastRestartReplacementSession(
                sessionPayload, onSessionAcceptedCallback, onSuccessCallback, onFailureCallback);
            TraceFastRestartBufferCheck(sessionPayload, true);
            return true;
        }

        internal bool ReplaceSession(LuxoddSessionPayload sessionPayload, Action onSuccessCallback = null,
            Action<int, string> onFailureCallback = null, Action onSessionAcceptedCallback = null)
        {
            if (sessionPayload == null || sessionPayload.IsValid == false)
            {
                if (_isFastRestartInProgress)
                {
                    FailFastRestart(FastRestartInvalidSessionStatusCode,
                        "The replacement session payload is invalid.");
                }
                else
                {
                    InvokeFailureCallback(onFailureCallback, FastRestartInvalidSessionStatusCode,
                        "The replacement session payload is invalid.");
                }

                return false;
            }

            if (_sessionPayload != null && _sessionPayload.Matches(sessionPayload))
            {
                return false;
            }

            var fastRestartOwnershipTransferred = _isFastRestartInProgress ||
                                                   _isSessionARecoveryBlockedAfterFastRestart;
            _isSessionARecoveryBlockedAfterFastRestart = false;

            if (_isSessionReplacementInProgress && _sessionPayload != null)
            {
                FailSessionReplacement(FastRestartAlreadyPendingStatusCode,
                    "The replacement session was superseded before it connected.");
            }

            _onSessionReplacementConnectedCallback = onSuccessCallback;
            _onSessionReplacementErrorCallback = onFailureCallback;
            _isSessionReplacementInProgress = true;
            _urlMetadataOwnerRetired = true;

            if (_isFastRestartInProgress)
            {
                _fastRestartReplacementSessionReceived = true;
                _fastRestartReplacementConnected = false;
                _fastRestartHandoffDeadline = 0f;
            }

            if (fastRestartOwnershipTransferred)
            {
                FastRestartRecoveryOwnershipResolved?.Invoke(false);
            }

            var handoffAlreadyInvalidated = _sessionTransportInvalidated && _sessionPayload == null &&
                                            _isConnected == false && _isConnectionAttemptInProgress == false;
            if (handoffAlreadyInvalidated == false)
            {
                _sessionPayload = null;
                _sessionTransportInvalidated = true;
                ResetTransportGeneration(SessionReplacementStatusCode,
                    "The previous session was replaced.", true, true);
            }

            _sessionPayload = sessionPayload;
            onSessionAcceptedCallback?.Invoke();
            _sessionReplacementConnectionDeadline =
                Time.realtimeSinceStartup + SessionReplacementConnectionTimeoutSeconds;
            if (TryStartConnectionAttempt(true) == false)
            {
                FailSessionReplacement(TransportFailureStatusCode,
                    "The replacement session connection could not be started.");
                return false;
            }

            return true;
        }

        internal bool TryReconnectFromReconnectService()
        {
            if (_isFastRestartInProgress || _isSessionReplacementInProgress ||
                _isSessionARecoveryBlockedAfterFastRestart || _isConnected || CanStartConnectionAttempt() == false)
            {
                return false;
            }

            return TryStartConnectionAttempt(true);
        }

        public void CloseConnection()
        {
            ResetTransportGeneration(1001, "WebSocket connection closed.", true, true);
        }

        public void BackToSystemWithError(string message, string error)
        {
            FailRestartPreparation(TransportFailureStatusCode, "Session ended during Fast Restart preparation.");
            LoggerHelper.LogError(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(BackToSystemWithError)}] OK, message: {message}, error: {error}");
            _socketLibraryWrapper.NotifySessionEnd();
        }

        public void BackToSystem()
        {
            FailRestartPreparation(1001, "Session ended during Fast Restart preparation.");
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(BackToSystem)}] OK");
            _socketLibraryWrapper.NotifySessionEnd();
        }

        public void SendSessionOptionRestart(Action<SessionOptionAction> callback)
        {
            if (TryBeginLegacySessionOptionRequest() == false)
            {
                return;
            }

            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SendSessionOptionRestart)}] OK");
            _onSessionOptionCallback = callback;
#if UNITY_WEBGL && !UNITY_EDITOR
            _socketLibraryWrapper.SendSessionOptionsRestart();
#endif
        }

        public void SendSessionOptionFastRestart(Action<SessionOptionAction> callback,
            Action<int, string> failureCallback = null)
        {
            RequestFastRestartSessionOption(callback, failureCallback, false);
        }

        /// <summary>
        /// Opens one Continue, Fast Restart, or End popup. Continue retains the current session
        /// but does not wait for deferred reconnects. Restart completes only when the replacement is ready.
        /// Host actions have no request identifier, so late results across separate popups cannot be identified.
        /// </summary>
        public void SendSessionOptionFastRestartWithContinue(Action<SessionOptionAction> callback,
            Action<int, string> failureCallback = null)
        {
            RequestFastRestartSessionOption(callback, failureCallback, true);
        }

        /// <summary>
        /// Opts into preparation before Restart. The game must explicitly Complete or Fail its context.
        /// Preparation runs on Unity's main thread with Session A active; Restart results still wait for Session B.
        /// </summary>
        public void SendSessionOptionFastRestartWithContinuePrepared(
            Action<FastRestartPreparationContext> onPrepare, Action<SessionOptionAction> onResult,
            Action<int, string> onFailure = null)
        {
            if (onPrepare == null)
            {
                InvokeFailureCallback(onFailure, FastRestartInvalidSessionStatusCode,
                    "Fast Restart preparation requires a preparation callback.");
                return;
            }

            RequestFastRestartSessionOption(onResult, onFailure, true, onPrepare);
        }

        private void RequestFastRestartSessionOption(Action<SessionOptionAction> callback,
            Action<int, string> failureCallback, bool includeContinue,
            Action<FastRestartPreparationContext> onPrepare = null)
        {
            if (_isFastRestartInProgress || _isSessionReplacementInProgress ||
                _isSessionARecoveryBlockedAfterFastRestart)
            {
                InvokeFailureCallback(failureCallback, FastRestartAlreadyPendingStatusCode,
                    "A Fast Restart session handoff is already in progress.");
                return;
            }

            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SendSessionOptionFastRestart)}] OK");
            _combinedSessionOptionOwnsActions = includeContinue;
            _isFastRestartInProgress = true;
            _urlMetadataOwnerRetired = true;
            _fastRestartRestartSelected = false;
            _fastRestartReplacementSessionReceived = false;
            _fastRestartReplacementConnected = false;
            _fastRestartHandoffDeadline = 0f;
            _onFastRestartCallback = callback;
            _onFastRestartFailureCallback = failureCallback;
            _restartPreparationPopupId++;
            _onFastRestartPreparation = onPrepare;
            _restartPreparationReady = false;

            _fastRestartTraceSessionA = _sessionPayload ?? LuxoddSessionBridge.Instance?.SessionPayload;
            _fastRestartTraceTransportOwner = _isConnected || _isConnectionAttemptInProgress ? "A" : "Unknown";
            Debug.Log($"[FastRestartTrace] RequestStarted fastRestartInProgress={_isFastRestartInProgress} " +
                      $"restartSelected={_fastRestartRestartSelected} replacementInProgress={_isSessionReplacementInProgress} " +
                      $"currentSessionPresent={_sessionPayload != null} connected={_isConnected} " +
                      $"hasBufferedReplacement={_bufferedFastRestartReplacementSession != null} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (onPrepare != null)
                {
                    _socketLibraryWrapper.SendSessionOptionsFastRestartWithContinuePrepared();
                }
                else if (includeContinue)
                {
                    _socketLibraryWrapper.SendSessionOptionsFastRestartWithContinue();
                }
                else
                {
                    _socketLibraryWrapper.SendSessionOptionsFastRestart();
                }
            }
            catch (Exception exception)
            {
                var reason = $"Fast Restart request could not be sent ({exception.GetType().Name}).";
                if (onPrepare != null)
                    FailRestartPreparation(TransportFailureStatusCode, reason);
                else
                    FailFastRestart(TransportFailureStatusCode, reason);
            }
#endif
        }

        public void SendSessionOptionContinue(Action<SessionOptionAction> callback)
        {
            if (TryBeginLegacySessionOptionRequest() == false)
            {
                return;
            }

            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SendSessionOptionContinue)}] OK");
            _onSessionOptionCallback = callback;
#if UNITY_WEBGL && !UNITY_EDITOR
            _socketLibraryWrapper.SendSessionOptionsContinue();
#endif
        }

        public void SendSessionOptionEnd(Action<SessionOptionAction> callback)
        {
            if (TryBeginLegacySessionOptionRequest() == false)
            {
                return;
            }

            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SendSessionOptionEnd)}] OK");

            _onSessionOptionCallback = callback;
#if UNITY_WEBGL && !UNITY_EDITOR
            _socketLibraryWrapper.SendSessionOptionsEnd();
#endif
        }

        private bool TryBeginLegacySessionOptionRequest()
        {
            if (_combinedSessionOptionOwnsActions && _isFastRestartInProgress)
            {
                Debug.LogWarning("[WebSocketService] Session option request rejected: a combined request is active.");
                return false;
            }

            _combinedSessionOptionOwnsActions = false;
            return true;
        }

        public void NotifyPrizeWon(string ticketsJson)
        {
            _socketLibraryWrapper.NotifyPrizeWon(ticketsJson);
        }

        public void SendCommand(CommandRequestType commandRequestType, string commandRequestJson,
            Action<CommandRequestHandler> onSuccess)
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(SendCommand)}] OK, _isConnected: {_isConnected} {commandRequestType}");
            if (commandRequestType == CommandRequestType.ScoreUpdateRequest)
            {
                FailCommand(new CommandRequestHandler
                {
                    CommandRequestType = commandRequestType,
                    OnCommandResponseSuccessHandler = onSuccess
                }, 400, "Use SendScoreUpdateRequestCommand for correlated score updates.");
                return;
            }
            if (ShouldRejectCommandDuringSessionHandoff())
            {
                var rejectedCommand = new CommandRequestHandler
                {
                    CommandRequestType = commandRequestType,
                    TransportGeneration = _transportGeneration,
                    OnCommandResponseSuccessHandler = onSuccess
                };
                FailCommand(rejectedCommand, SessionReplacementStatusCode,
                    "Command rejected while the session is being replaced.");
                return;
            }

            if (_isConnected)
            {
                ProcessCommandInner(commandRequestType, commandRequestJson, onSuccess);
            }
            else
            {
                AddCommandToDispatch(commandRequestType, commandRequestJson, onSuccess);
                EnsureWaitForConnection();
            }

        }

        private void Awake()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (_isApplicationQuitting == false)
            {
                HandleApplicationClosing();
            }

            UnsubscribeFromEvents();
        }

        private void Update()
        {
            ProcessScoreUpdateTimeout(Time.realtimeSinceStartup);
            ProcessCommandResponseTimeouts(Time.realtimeSinceStartup);
            ProcessFastRestartHandoffTimeout(Time.realtimeSinceStartup);
            ProcessRestartPreparationTimeout(Time.realtimeSinceStartup);
        }

        private void SubscribeToEvents()
        {
            _socketLibraryWrapper.WebSocketOpenedEvent.AddListener(OnWebSocketConnectedHandler);
            _socketLibraryWrapper.WebSocketConnectionErrorEvent.AddListener(OnWebSocketConnectionErrorHandler);
            _socketLibraryWrapper.WebSocketClosedEvent.AddListener(OnWebSocketClosedHandler);
            _socketLibraryWrapper.MessageReceivedEvent.AddListener(OnMessageReceived);
            
            _socketLibraryWrapper.OnSessionOptionAction.AddListener(OnSessionOptionsCallback);
            _socketLibraryWrapper.OnRestartPreparationRequested.AddListener(OnRestartPreparationRequested);
        }

        private async void ProcessCommandInner(CommandRequestType commandRequestType, string commandRequestJson,
            Action<CommandRequestHandler> onSuccess)
        {
            var commandRequestHandler = AddCommandRequestHandler(commandRequestType, onSuccess);
            var wasSent = await SendEventInner(commandRequestJson, commandRequestHandler);

            if (commandRequestHandler.IsCompleted ||
                commandRequestHandler.TransportGeneration != _transportGeneration)
            {
                return;
            }

            if (wasSent == false)
            {
                ResetTransportGeneration(TransportFailureStatusCode, "WebSocket send failed.", true, false);
                return;
            }

            commandRequestHandler.ResponseDeadline =
                Time.realtimeSinceStartup + CommandResponseTimeoutSeconds;
        }

        private void OnWebSocketConnectionErrorHandler(string error)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketConnectionErrorHandler)}] WebSocket connection error received.");
            if (_transportCloseRequested)
            {
                // WebGL emits error before close for an intentionally closed CONNECTING socket.
                // Keep ownership until the matching close callback consumes the marker.
                SetConnectedState(false);
                return;
            }

            var failedSessionReplacement = _isSessionReplacementInProgress;
            ResetTransportGeneration(TransportFailureStatusCode,
                string.IsNullOrEmpty(error) ? "WebSocket connection error." : error,
                true, failedSessionReplacement);
            if (failedSessionReplacement)
            {
                FailSessionReplacement(TransportFailureStatusCode,
                    string.IsNullOrEmpty(error) ? "Replacement session connection failed." : error);
            }
        }

        private void OnWebSocketClosedHandler(int code)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketConnectionErrorHandler)}] OK");
            if (_transportCloseRequested)
            {
                _transportCloseRequested = false;
                SetConnectedState(false);
                return;
            }

            var failedSessionReplacement = _isSessionReplacementInProgress;
            ResetTransportGeneration(code == 0 ? TransportFailureStatusCode : code,
                "WebSocket connection closed.", false, failedSessionReplacement);
            if (failedSessionReplacement)
            {
                FailSessionReplacement(code == 0 ? TransportFailureStatusCode : code,
                    "Replacement session connection closed before it opened.");
            }
#if UNITY_EDITOR
            if (failedSessionReplacement == false)
            {
                ScheduleEditorReconnect();
            }
#endif
        }

        private void OnWebSocketClosedForGeneration(int code, int closeGeneration)
        {
            if (closeGeneration != _transportGeneration)
            {
                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketClosedForGeneration)}] Ignoring stale transport generation: {closeGeneration}");
                return;
            }

            OnWebSocketClosedHandler(code);
        }

        private void UnsubscribeFromEvents()
        {
            _socketLibraryWrapper.WebSocketOpenedEvent.RemoveListener(OnWebSocketConnectedHandler);
            _socketLibraryWrapper.WebSocketConnectionErrorEvent.RemoveListener(OnWebSocketConnectionErrorHandler);
            _socketLibraryWrapper.WebSocketClosedEvent.RemoveListener(OnWebSocketClosedHandler);
            _socketLibraryWrapper.MessageReceivedEvent.RemoveListener(OnMessageReceived);
            _socketLibraryWrapper.OnSessionOptionAction.RemoveListener(OnSessionOptionsCallback);
            _socketLibraryWrapper.OnRestartPreparationRequested.RemoveListener(OnRestartPreparationRequested);
        }

        private string GetSessionToken()
        {
#if UNITY_EDITOR
            if (_sessionPayload != null && _sessionPayload.IsValid)
            {
                return _sessionPayload.Token;
            }

            return _settingsDescriptor != null ? _settingsDescriptor.DeveloperDebugToken : string.Empty;
#else
            if (_isSessionReplacementInProgress && _sessionPayload == null)
            {
                return string.Empty;
            }

            if (_sessionPayload != null && _sessionPayload.IsValid)
            {
                return _sessionPayload.Token;
            }

            return _fetchUrlQueryString != null ? _fetchUrlQueryString.Token : string.Empty;
#endif
        }

        private async Task StartConnectionAsync(int connectionAttemptId, bool isInternalRecovery)
        {
            var safeEndpoint = "WebSocket endpoint";
            try
            {
                await Task.Yield();
                if (IsCurrentConnectionAttempt(connectionAttemptId) == false)
                {
                    return;
                }

                var isDebug = false;
                var serverUrlRaw = _fetchUrlQueryString.WSUrl;
                if (string.IsNullOrEmpty(serverUrlRaw))
                {
                    serverUrlRaw = _settingsDescriptor.ServerAddress;
                }
#if UNITY_EDITOR
                isDebug = true;
#endif

                var host = _webGlHostWrapper.GetParentHostSafe();
                var protocol = _webGlHostWrapper.GetWebSocketProtocolSafe();
                var shouldUseHost = string.IsNullOrEmpty(host) == false && string.IsNullOrEmpty(protocol) == false && host.Contains("localhost") == false;
                var fullUrl = shouldUseHost ? $"{protocol}//{host}/ws" : string.Empty;
                var token = isDebug == false ? _fetchUrlQueryString.Token : _settingsDescriptor.DeveloperDebugToken;

                var serverUrl = $"{serverUrlRaw}?token={token}";

                if (shouldUseHost)
                {
                    serverUrl = $"{fullUrl}?token={token}";
                }

                var traceAttemptSessionPayload = _sessionPayload;
                if (_sessionPayload != null)
                {
                    serverUrl = $"{_sessionPayload.WsUrl}?token={_sessionPayload.Token}";
                }

                var websocketUri = new Uri(serverUrl);
                safeEndpoint = GetSafeWebSocketEndpoint(websocketUri);
                if (_sessionPayload == null)
                    ObserveUrlMetadataOwner(token, websocketUri.GetLeftPart(UriPartial.Path));

                Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartConnectionAsync)}] Connecting to: {safeEndpoint}");


#if !UNITY_EDITOR
                await Task.Yield();
                if (IsCurrentConnectionAttempt(connectionAttemptId) == false)
                {
                    return;
                }

                var connectionGeneration = BeginTransportGeneration();
                // Ignore only the close callback for the transport deliberately closed by the prior reset.
                // The WebGL bridge also rejects callbacks from any socket that is no longer current.
                _transportCloseRequested = false;
                _socketLibraryWrapper?.SetCurrentTransportIdentity(connectionGeneration, connectionAttemptId);
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartConnectionAsync)}] Connecting to: {safeEndpoint}");
                TraceFastRestartTransportStart(traceAttemptSessionPayload, connectionAttemptId, connectionGeneration);
                _socketLibraryWrapper.StartWebSocket(websocketUri.AbsoluteUri, connectionGeneration,
                    connectionAttemptId);
#else
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartConnectionAsync)}] Connecting to: {safeEndpoint}");

                var connectionGeneration = BeginTransportGeneration();
                _socketLibraryWrapper?.SetCurrentTransportIdentity(connectionGeneration, connectionAttemptId);
                TraceFastRestartTransportStart(traceAttemptSessionPayload, connectionAttemptId, connectionGeneration);
#if UNITY_EDITOR
                if (_connectionAttemptTestHook != null)
                {
                    _clientWebSocket = null;
                    _connectionAttemptTestHook(websocketUri);
                    return;
                }
#endif
                var clientWebSocket = new ClientWebSocket();
                _clientWebSocket = clientWebSocket;
                await clientWebSocket.ConnectAsync(websocketUri, CancellationToken.None);

                if (IsCurrentConnectionAttempt(connectionAttemptId) == false ||
                    IsCurrentClientWebSocket(clientWebSocket, connectionGeneration) == false)
                {
                    clientWebSocket.Dispose();
                    return;
                }

                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(StartConnectionAsync)}] Connected to: {safeEndpoint}");
                CompleteConnectionAttempt(connectionAttemptId, isInternalRecovery);
                _ = ReceiveMessage(clientWebSocket, connectionGeneration);
#endif
            }
            catch (Exception exception)
            {
                if (IsCurrentConnectionAttempt(connectionAttemptId) == false)
                {
                    return;
                }

                LoggerHelper.LogError(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(StartConnectionAsync)}] Connection failed " +
                    $"({exception.GetType().Name}) for {safeEndpoint}.");
                var failedSessionReplacement = _isSessionReplacementInProgress;
                ResetTransportGeneration(TransportFailureStatusCode, "WebSocket connection failed.", true,
                    failedSessionReplacement);
                if (failedSessionReplacement)
                {
                    FailSessionReplacement(TransportFailureStatusCode, "Replacement session connection failed.");
                }
            }
        }

        private void OnMessageReceived(string message)
        {
            //LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnMessageReceived)}] " + message);
            OnMessageReceivedForGeneration(message, _transportGeneration);
        }

        private void OnMessageReceivedForGeneration(string message, int messageGeneration)
        {
            if (UnityMainThread.Worker == null)
            {
                HandleMessageReceivedForGeneration(message, messageGeneration);
                return;
            }

            UnityMainThread.Worker.AddJob(() => HandleMessageReceivedForGeneration(message, messageGeneration));
        }

        private void OnApplicationQuit()
        {
            HandleApplicationClosing();
        }

        private async Task ReceiveMessage(ClientWebSocket clientWebSocket, int connectionGeneration)
        {
            var buffer = new byte[1024 * 4];
            while (IsCurrentClientWebSocket(clientWebSocket, connectionGeneration) &&
                   clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result =
                        await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (IsCurrentClientWebSocket(clientWebSocket, connectionGeneration) == false)
                    {
                        return;
                    }

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                        {
                            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            LoggerHelper.Log(
                                $"[{DateTime.Now}][{GetType().Name}][{nameof(ReceiveMessage)}] WebSocket message received.");
                            OnMessageReceivedForGeneration(receivedMessage, connectionGeneration);
                            //UnityMainThread.Worker.AddJob(() => _successCallback?.Invoke(receivedMessage));

                            break;
                        }
                        case WebSocketMessageType.Close:
                            LoggerHelper.Log(
                                $"[{DateTime.Now}][{GetType().Name}][{nameof(ReceiveMessage)}] OK, closed");
                            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                                CancellationToken.None);
                            OnWebSocketClosedForGeneration((int)WebSocketCloseStatus.NormalClosure, connectionGeneration);
                            break;
                        case WebSocketMessageType.Binary:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                    if (IsCurrentClientWebSocket(clientWebSocket, connectionGeneration) == false)
                    {
                        return;
                    }

                    LoggerHelper.LogError(
                        $"[{GetType().Name}][{nameof(ReceiveMessage)}] WebSocket receive failed ({ex.GetType().Name}).");
                    OnWebSocketConnectionErrorHandler(ex.Message);
                    return;
                }
            }
        }

        private async Task<bool> SendMessageWebsocket(string message, CommandRequestHandler commandRequestHandler)
        {
            if (CanSendCommand(commandRequestHandler) == false)
            {
                return false;
            }

#if UNITY_EDITOR
            if (_sendMessageTestHook != null)
            {
                return await _sendMessageTestHook(message);
            }
#endif

            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
            {
                LoggerHelper.LogError($"[{GetType().Name}][{nameof(SendMessageWebsocket)}] OK, not connected");
                _errorCallback?.Invoke($"[{GetType().Name}][{nameof(SendMessageWebsocket)}] Not connected");
                // The service connection flag and the managed socket can briefly disagree during
                // reconnect. Keep the request in the current generation so the response timeout
                // remains the final bounded failure path.
                return true;
            }

            try
            {
                var clientWebSocket = _clientWebSocket;
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                    CancellationToken.None);

                if (ReferenceEquals(clientWebSocket, _clientWebSocket) == false ||
                    commandRequestHandler.TransportGeneration != _transportGeneration)
                {
                    return false;
                }

                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(SendMessageWebsocket)}] WebSocket message sent.");
                return true;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(
                    $"[{GetType().Name}][{nameof(SendMessageWebsocket)}] WebSocket send failed ({ex.GetType().Name}).");
                return false;
            }
        }

        private bool CanSendCommand(CommandRequestHandler commandRequestHandler)
        {
            return commandRequestHandler.IsCompleted == false &&
                   commandRequestHandler.TransportGeneration == _transportGeneration &&
                   _isConnected && _isApplicationQuitting == false;
        }

        private async Task<bool> SendEventInner(string commandRequestRaw, CommandRequestHandler commandRequestHandler)
        {
            if (CanSendCommand(commandRequestHandler) == false)
            {
                return false;
            }

#if !UNITY_EDITOR
            try
            {
                if (_isConnected == false)
                {
                    LoggerHelper.LogError($"[{GetType().Name}][{nameof(SendEventInner)}] Not connected");
                    return false;
                }

                await Task.Yield();

                if (CanSendCommand(commandRequestHandler) == false)
                {
                    return false;
                }

                _socketLibraryWrapper.SendMessageToWebSocket(commandRequestRaw);
                return true;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"[{GetType().Name}][{nameof(SendEventInner)}] WebSocket send failed ({ex.GetType().Name}).");
                return false;
            }
#else
            return await SendMessageWebsocket(commandRequestRaw, commandRequestHandler);
#endif
        }

        private void OnWebSocketConnectedHandler()
        {
            // An attempt can be reserved before its transport replaces the wrapper's previous identity.
            if (_socketLibraryWrapper == null ||
                _socketLibraryWrapper.MatchesTransportIdentity(_transportGeneration, _connectionAttemptId) == false)
            {
                return;
            }

            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketConnectedHandler)}] OK");
            CompleteConnectionAttempt(_connectionAttemptId, _currentAttemptIsInternalRecovery);
        }

        private void CompleteConnectionAttempt(int connectionAttemptId, bool isInternalRecovery)
        {
            if (IsCurrentConnectionAttempt(connectionAttemptId) == false)
            {
                return;
            }

            _isConnectionAttemptInProgress = false;
            _currentAttemptIsInternalRecovery = false;
            SetConnectedState(true);

            if (isInternalRecovery == false)
            {
                _onConnectedCallback?.Invoke();
            }

            if (_isSessionReplacementInProgress)
            {
                _isSessionReplacementInProgress = false;
                _sessionReplacementConnectionDeadline = 0f;
                _sessionTransportInvalidated = false;
                var onSessionReplacementConnected = _onSessionReplacementConnectedCallback;
                _onSessionReplacementConnectedCallback = null;
                _onSessionReplacementErrorCallback = null;
                onSessionReplacementConnected?.Invoke();

                if (_isFastRestartInProgress)
                {
                    _fastRestartReplacementConnected = true;
                    TryCompleteFastRestart();
                }
            }

            StartFlushCommands();
        }

        private void SetConnectedState(bool isConnected)
        {
            if (_isConnected == isConnected)
            {
                return;
            }

            _isConnected = isConnected;
            if (isConnected == false)
            {
                FailRestartPreparation(TransportFailureStatusCode,
                    "Session A connection was lost during Fast Restart preparation.");
                var replacementConnectionLost = _isFastRestartInProgress && _fastRestartReplacementConnected;
                _fastRestartReplacementConnected = false;
                if (replacementConnectionLost)
                {
                    FailFastRestart(TransportFailureStatusCode,
                        "The replacement session connection was lost before Fast Restart completed.");
                }
            }

            _isConnectedEvent.Notify(_isConnected);
        }

        private CommandRequestHandler AddCommandRequestHandler(CommandRequestType commandRequestType,
            Action<CommandRequestHandler> onSuccess)
        {
            var commandRequestHandler = new CommandRequestHandler()
            {
                CommandRequestType = commandRequestType,
                Id = 0,
                TransportGeneration = _transportGeneration,
                OnCommandResponseSuccessHandler = onSuccess,
            };

            if (_commandRequestHandlers.TryGetValue(commandRequestType, out var queueOfCommandRequestHandlers))
            {
                commandRequestHandler.Id = queueOfCommandRequestHandlers.Count;
                queueOfCommandRequestHandlers.Enqueue(commandRequestHandler);
            }
            else
            {
                var queue = new Queue<CommandRequestHandler>();
                queue.Enqueue(commandRequestHandler);
                _commandRequestHandlers.Add(commandRequestType, queue);
            }

            return commandRequestHandler;
        }

        private void HandleMessageReceived(string message)
        {
            HandleMessageReceivedForGeneration(message, _transportGeneration);
        }

        private void HandleMessageReceivedForGeneration(string message, int messageGeneration)
        {
#if NEWTONSOFT_JSON
            if (messageGeneration != _transportGeneration)
            {
                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleMessageReceived)}] Ignoring stale transport generation: {messageGeneration}");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                LoggerHelper.LogWarning(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleMessageReceived)}] Ignoring empty WebSocket message.");
                return;
            }

            CommandResponse responseJson;
            try
            {
                responseJson = JsonConvert.DeserializeObject<CommandResponse>(message);
            }
            catch (JsonException)
            {
                LoggerHelper.LogWarning(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleMessageReceived)}] Ignoring malformed WebSocket message.");
                return;
            }

            if (responseJson == null) return;
            if (string.Equals(responseJson.Type, "score_update_response", StringComparison.Ordinal))
            {
                HandleScoreUpdateResponse(responseJson, message, messageGeneration);
                return;
            }
            if (string.IsNullOrWhiteSpace(responseJson.Type))
            {
                LoggerHelper.LogWarning(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleMessageReceived)}] Ignoring WebSocket message without a response type.");
                return;
            }

            if (responseJson.Type.Contains("_"))
            {
                responseJson.Type = responseJson.Type.ToPascalCaseStyle();
            }

            if (Enum.TryParse<CommandResponseType>(responseJson.Type, out var commandResponseType) == false) return;

            var commandRequestType = commandResponseType.ToCommandRequestType();

            if (_commandRequestHandlers.TryGetValue(commandRequestType, out var queueOfCommandRequestHandlers) ==
                false) return;

            if (queueOfCommandRequestHandlers.Count > 0)
            {
                var commandRequestHandler = queueOfCommandRequestHandlers.Peek();
                if (commandRequestHandler.TransportGeneration != messageGeneration ||
                    commandRequestHandler.IsCompleted)
                {
                    return;
                }

                queueOfCommandRequestHandlers.Dequeue();
                commandRequestHandler.CommandResponse = responseJson;
                commandRequestHandler.RawResponse = message;
                commandRequestHandler.IsCompleted = true;
                if (queueOfCommandRequestHandlers.Count == 0)
                {
                    _commandRequestHandlers.Remove(commandRequestType);
                }

                commandRequestHandler.OnCommandResponseSuccessHandler?.Invoke(commandRequestHandler);
            }
            else
            {
                LoggerHelper.LogError(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(HandleMessageReceived)}] Received a response without a pending command.");
            }
#endif
        }

        private int BeginTransportGeneration()
        {
            _transportGeneration++;
            return _transportGeneration;
        }

#if NEWTONSOFT_JSON
        private void HandleScoreUpdateResponse(CommandResponse response, string raw, int generation)
        {
            var request = _scoreUpdateRequest;
            if (request == null || request.IsCompleted || request.TransportGeneration != generation) return;
            Payloads.ScoreUpdateResponse payload;
            try
            {
                payload = JsonConvert.DeserializeObject<Payloads.ScoreUpdateResponse>(
                    JsonConvert.SerializeObject(response.Payload));
            }
            catch (JsonException)
            {
                Debug.LogWarning("[ScoreUpdate] Ignoring response without a valid request_id.");
                return;
            }
            if (payload == null || payload.RequestId != request.Id)
            {
                Debug.LogWarning("[ScoreUpdate] Ignoring response with an unmatched request_id.");
                return;
            }

            _scoreUpdateRequest = null;
            request.IsCompleted = true;
            response.Payload = payload;
            request.CommandResponse = response;
            request.RawResponse = raw;
            request.ErrorMessage = response.StatusCode == 200 ? null : "Score update rejected by server: " + payload.Result;
            request.OnCommandResponseSuccessHandler?.Invoke(request);
        }
#endif

        private bool CanStartConnectionAttempt()
        {
            return _isConnectionAttemptInProgress == false && _isResettingTransport == false &&
                   _isApplicationQuitting == false;
        }

        private bool TryStartConnectionAttempt(bool isInternalRecovery)
        {
            if (isInternalRecovery && CanStartInternalRecovery() == false)
            {
                return false;
            }

            if (CanStartConnectionAttempt() == false)
            {
                return false;
            }

            _connectionAttemptId++;
            _isConnectionAttemptInProgress = true;
            _currentAttemptIsInternalRecovery = isInternalRecovery;
            if (_isFastRestartInProgress || _isSessionReplacementInProgress)
            {
                Debug.Log($"[FastRestartTrace] ConnectionAttemptStart owner={GetFastRestartTraceOwner(_sessionPayload)} " +
                          $"attemptId={_connectionAttemptId} transportGeneration={_transportGeneration} " +
                          $"replacementInProgress={_isSessionReplacementInProgress} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
            }
            _ = StartConnectionAsync(_connectionAttemptId, isInternalRecovery);
            return true;
        }

        private bool CanStartInternalRecovery()
        {
            if (_isSessionReplacementInProgress)
            {
                // A replacement connection may recover only after its authoritative payload
                // has transferred ownership from the prior session.
                return _sessionPayload != null;
            }

            return _isFastRestartInProgress == false &&
                   _isSessionARecoveryBlockedAfterFastRestart == false;
        }

        private bool IsCurrentConnectionAttempt(int connectionAttemptId)
        {
            return connectionAttemptId == _connectionAttemptId && _isConnectionAttemptInProgress &&
                   _isApplicationQuitting == false;
        }

        private static string GetSafeWebSocketEndpoint(Uri websocketUri)
        {
            return websocketUri.GetLeftPart(UriPartial.Path);
        }

        private bool IsCurrentClientWebSocket(ClientWebSocket clientWebSocket, int connectionGeneration)
        {
            return ReferenceEquals(_clientWebSocket, clientWebSocket) &&
                   connectionGeneration == _transportGeneration &&
                   _isApplicationQuitting == false;
        }

        private void ProcessCommandResponseTimeouts(float now)
        {
            foreach (var requestQueue in _commandRequestHandlers.Values)
            {
                foreach (var commandRequestHandler in requestQueue)
                {
                    if (commandRequestHandler.IsCompleted == false &&
                        commandRequestHandler.ResponseDeadline > 0f &&
                        commandRequestHandler.ResponseDeadline <= now)
                    {
                        ResetTransportGeneration(CommandResponseTimeoutStatusCode,
                            "Timed out waiting for a WebSocket command response.", true, true, true);
                        return;
                    }
                }
            }
        }

        private void ResetTransportGeneration(int statusCode, string errorMessage, bool closeSocket,
            bool clearQueuedCommands, bool reconnectAfterReset = false)
        {
            if (_isResettingTransport)
            {
                return;
            }

            var shouldReconnect = reconnectAfterReset && _isApplicationQuitting == false &&
                                  isActiveAndEnabled && CanStartInternalRecovery();
            _isResettingTransport = true;
            if (statusCode == SessionReplacementStatusCode)
            {
                try { SessionInvalidated?.Invoke(); }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[ScoreUpdate] Session invalidation callback threw ({exception.GetType().Name}).");
                }
            }
            var pendingScoreUpdate = _scoreUpdateRequest;
            _scoreUpdateRequest = null;
            BeginTransportGeneration();
            _connectionAttemptId++;
            _isConnectionAttemptInProgress = shouldReconnect;
            _currentAttemptIsInternalRecovery = shouldReconnect;
            _isInReconnection = false;
            _isInFlushing = false;

            if (_waitForConnectionCoroutine != null)
            {
                StopCoroutine(_waitForConnectionCoroutine);
                _waitForConnectionCoroutine = null;
            }

            if (_flushCommandsCoroutine != null)
            {
                StopCoroutine(_flushCommandsCoroutine);
                _flushCommandsCoroutine = null;
            }

            var pendingHandlers = new List<CommandRequestHandler>();
            foreach (var requestQueue in _commandRequestHandlers.Values)
            {
                pendingHandlers.AddRange(requestQueue);
            }

            _commandRequestHandlers.Clear();

            var queuedCommands = clearQueuedCommands
                ? new List<SendCommandData>(_sendCommandDataQueue)
                : new List<SendCommandData>();
            if (clearQueuedCommands)
            {
                _sendCommandDataQueue.Clear();
            }

            if (closeSocket)
            {
                if (statusCode == SessionReplacementStatusCode && _isFastRestartInProgress && _fastRestartRestartSelected)
                {
                    Debug.Log($"[FastRestartTrace] ClosingTransportForRestart owner={_fastRestartTraceTransportOwner} " +
                              $"attemptId={_fastRestartTraceTransportAttemptId} transportGeneration={_fastRestartTraceTransportGeneration} " +
                              $"replacementInProgress={_isSessionReplacementInProgress} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
                }
                CloseCurrentSocket();
            }

            SetConnectedState(false);

            FailCommand(pendingScoreUpdate, statusCode, errorMessage);

            foreach (var commandRequestHandler in pendingHandlers)
            {
                FailCommand(commandRequestHandler, statusCode, errorMessage);
            }

            foreach (var queuedCommand in queuedCommands)
            {
                var commandRequestHandler = new CommandRequestHandler
                {
                    CommandRequestType = queuedCommand.CommandRequestType,
                    TransportGeneration = _transportGeneration,
                    OnCommandResponseSuccessHandler = queuedCommand.OnCommandResponseSuccessHandler
                };
                FailCommand(commandRequestHandler, statusCode, errorMessage);
            }

#if UNITY_EDITOR
            if (closeSocket && clearQueuedCommands == false && shouldReconnect == false &&
                _isApplicationQuitting == false)
            {
                ScheduleEditorReconnect();
            }
#endif

            _isResettingTransport = false;
            if (shouldReconnect)
            {
                _ = StartConnectionAsync(_connectionAttemptId, true);
            }
        }

        private void FailCommand(CommandRequestHandler commandRequestHandler, int statusCode, string errorMessage)
        {
            if (commandRequestHandler == null || commandRequestHandler.IsCompleted)
            {
                return;
            }

            var commandResponseType = commandRequestHandler.CommandRequestType.ToCommandResponseType();
            commandRequestHandler.CommandResponse = new CommandResponse
            {
                MessageVersion = "1",
                Type = commandResponseType.ToString(),
                StatusCode = statusCode,
                Payload = null
            };
            commandRequestHandler.RawResponse =
                $"{{\"type\":\"{commandResponseType}\",\"msgver\":\"1\",\"status\":{statusCode},\"payload\":null}}";
            commandRequestHandler.ErrorMessage = errorMessage;
            commandRequestHandler.IsCompleted = true;
            try
            {
                commandRequestHandler.OnCommandResponseSuccessHandler?.Invoke(commandRequestHandler);
            }
            catch (Exception exception)
            {
                LoggerHelper.LogError(
                    $"[{GetType().Name}][{nameof(FailCommand)}] Command failure callback threw ({exception.GetType().Name}).");
            }
        }

        private void CloseCurrentSocket()
        {
            _transportCloseRequested = true;
#if !UNITY_EDITOR
            try
            {
                _socketLibraryWrapper?.CloseWebSocketConnection();
            }
            catch (Exception exception)
            {
                LoggerHelper.LogError(
                    $"[{GetType().Name}][{nameof(CloseCurrentSocket)}] WebSocket close failed ({exception.GetType().Name}).");
                _transportCloseRequested = false;
            }
#else
            var clientWebSocket = _clientWebSocket;
            _clientWebSocket = null;
            if (clientWebSocket != null)
            {
                try
                {
                    if (clientWebSocket.State == WebSocketState.Open ||
                        clientWebSocket.State == WebSocketState.CloseReceived)
                    {
                        clientWebSocket
                            .CloseAsync(WebSocketCloseStatus.NormalClosure, "Reset transport", CancellationToken.None)
                            .Wait();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError(
                        $"[{GetType().Name}][{nameof(CloseCurrentSocket)}] WebSocket close failed ({ex.GetType().Name}).");
                }
                finally
                {
                    clientWebSocket.Dispose();
                }
            }

            _transportCloseRequested = false;
#endif
        }

#if UNITY_EDITOR
        private void ScheduleEditorReconnect()
        {
            if (_editorReconnectScheduled || _isApplicationQuitting || isActiveAndEnabled == false)
            {
                return;
            }

            _editorReconnectScheduled = true;
            CoroutineManager.DelayedAction(_timeBeforeReconnect, () =>
            {
                _editorReconnectScheduled = false;
                if (_isConnected == false && _isConnectionAttemptInProgress == false &&
                    _isApplicationQuitting == false)
                {
                    TryStartConnectionAttempt(true);
                }
            });
        }
#endif

        //if connected - OK
        //if was connected and now disconnected - add to query
        //if was disconnected and now connected

        private void EnsureWaitForConnection()
        {
            if (_isInReconnection || _waitForConnectionCoroutine != null ||
                isActiveAndEnabled == false || _isApplicationQuitting)
            {
                return;
            }

            _waitForConnectionCoroutine = StartCoroutine(WaitForConnection());
        }

        private IEnumerator WaitForConnection()
        {
            Debug.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(WaitForConnection)}] OK");
            _isInReconnection = true;
            var deadline = Time.time + _secondsBeforeError;

            while (_isConnected == false && Time.time < deadline)
            {
                yield return new WaitForSeconds(0.3f);
            }

            _isInReconnection = false;
            _waitForConnectionCoroutine = null;

            Debug.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(WaitForConnection)}] OK, _isConnected={_isConnected}");

            if (_isConnected)
            {
                StartFlushCommands();
            }
            else if (Time.time >= deadline)
            {
                FailQueuedCommands(TransportFailureStatusCode, "Connection error.");
                BackToSystemWithError("Can't connect to server", "Connection error");
            }
        }

        private void StartFlushCommands()
        {
            if (_sendCommandDataQueue.Count == 0 || _isInFlushing || isActiveAndEnabled == false)
            {
                return;
            }

            _flushCommandsCoroutine = StartCoroutine(FlushCommands());
        }

        private IEnumerator FlushCommands()
        {
            Debug.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(FlushCommands)}] OK, total commands: {_sendCommandDataQueue.Count}");
            _isInFlushing = true;
            while (_sendCommandDataQueue.Count > 0)
            {
                var command = _sendCommandDataQueue.Dequeue();
                ProcessCommandInner(command.CommandRequestType, command.CommandRequestJson,
                    command.OnCommandResponseSuccessHandler);
                yield return new WaitForSeconds(0.3f);
            }

            _isInFlushing = false;
            _flushCommandsCoroutine = null;
        }

        private void FailQueuedCommands(int statusCode, string errorMessage)
        {
            while (_sendCommandDataQueue.Count > 0)
            {
                var queuedCommand = _sendCommandDataQueue.Dequeue();
                var commandRequestHandler = new CommandRequestHandler
                {
                    CommandRequestType = queuedCommand.CommandRequestType,
                    TransportGeneration = _transportGeneration,
                    OnCommandResponseSuccessHandler = queuedCommand.OnCommandResponseSuccessHandler
                };
                FailCommand(commandRequestHandler, statusCode, errorMessage);
            }
        }

        private void AddCommandToDispatch(CommandRequestType commandRequestType, string commandRequestJson,
            Action<CommandRequestHandler> onSuccess)
        {
            Debug.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(AddCommandToDispatch)}] Queued command: {commandRequestType}.");

            var command = new SendCommandData()
            {
                CommandRequestType = commandRequestType,
                CommandRequestJson = commandRequestJson,
                OnCommandResponseSuccessHandler = onSuccess
            };
            _sendCommandDataQueue.Enqueue(command);
        }

        private bool ShouldRejectCommandDuringSessionHandoff()
        {
            return (_isSessionReplacementInProgress && _sessionPayload == null) ||
                   (_isFastRestartInProgress &&
                    (_fastRestartRestartSelected || _fastRestartReplacementSessionReceived));
        }

        private void ProcessFastRestartHandoffTimeout(float now)
        {
            if (_isSessionReplacementInProgress && _sessionReplacementConnectionDeadline > 0f &&
                now >= _sessionReplacementConnectionDeadline)
            {
                _sessionReplacementConnectionDeadline = 0f;
                ResetTransportGeneration(CommandResponseTimeoutStatusCode,
                    "Timed out connecting to the replacement session.", true, true);
                FailSessionReplacement(CommandResponseTimeoutStatusCode,
                    "Timed out connecting to the replacement session.");
                return;
            }

            if (_isFastRestartInProgress && _fastRestartRestartSelected &&
                _fastRestartReplacementSessionReceived == false && _fastRestartHandoffDeadline > 0f &&
                now >= _fastRestartHandoffDeadline)
            {
                _fastRestartHandoffDeadline = 0f;
                FailFastRestart(CommandResponseTimeoutStatusCode,
                    "Timed out waiting for the replacement session.");
            }
        }

        private void BeginFastRestartSessionHandoff()
        {
            if (_isSessionReplacementInProgress)
            {
                return;
            }

            _isSessionARecoveryBlockedAfterFastRestart = true;
            FastRestartRecoveryOwnershipResolved?.Invoke(false);
            _sessionTransportInvalidated = true;
            _sessionPayload = null;
            ResetTransportGeneration(SessionReplacementStatusCode,
                "The previous session was replaced.", true, true);
            _isSessionReplacementInProgress = true;
        }

        private void ConsumeBufferedFastRestartReplacementSession()
        {
            var bufferedReplacement = _bufferedFastRestartReplacementSession;
            _bufferedFastRestartReplacementSession = null;
            if (bufferedReplacement == null)
            {
                return;
            }

            Debug.Log($"[FastRestartTrace] BufferedReplacementConsumed serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
            ReplaceSession(bufferedReplacement.SessionPayload, bufferedReplacement.OnConnectedCallback,
                bufferedReplacement.OnFailureCallback, bufferedReplacement.OnSessionAcceptedCallback);
        }

        private void TryCompleteFastRestart()
        {
            if (_isFastRestartInProgress == false || _fastRestartRestartSelected == false ||
                _fastRestartReplacementSessionReceived == false || _fastRestartReplacementConnected == false ||
                _isConnected == false || _isConnectionAttemptInProgress || _isSessionReplacementInProgress)
            {
                return;
            }

            var callback = _onFastRestartCallback;
            ClearFastRestartState();
            try
            {
                callback?.Invoke(SessionOptionAction.Restart);
            }
            catch (Exception exception)
            {
                LoggerHelper.LogError(
                    $"[{GetType().Name}][{nameof(TryCompleteFastRestart)}] Success callback threw ({exception.GetType().Name}).");
            }
        }

        private void CompleteFastRestartWithAction(SessionOptionAction action)
        {
            var callback = _onFastRestartCallback;
            var canResumeSessionA = (action == SessionOptionAction.Continue ||
                                     action == SessionOptionAction.Cancel) &&
                                    _isSessionReplacementInProgress == false &&
                                    _fastRestartReplacementSessionReceived == false &&
                                    _sessionPayload != null;
            _isSessionARecoveryBlockedAfterFastRestart = canResumeSessionA == false;
            ClearFastRestartState();
            FastRestartRecoveryOwnershipResolved?.Invoke(canResumeSessionA);
            try
            {
                callback?.Invoke(action);
            }
            catch (Exception exception)
            {
                LoggerHelper.LogError(
                    $"[{GetType().Name}][{nameof(CompleteFastRestartWithAction)}] Result callback threw ({exception.GetType().Name}).");
            }
        }

        private void FailFastRestart(int statusCode, string errorMessage)
        {
            if (_isFastRestartInProgress == false)
            {
                return;
            }

            var callback = _onFastRestartFailureCallback;
            if (_fastRestartReplacementSessionReceived == false)
            {
                _isSessionARecoveryBlockedAfterFastRestart = true;
            }
            ClearFastRestartState();
            FastRestartRecoveryOwnershipResolved?.Invoke(false);
            InvokeFailureCallback(callback, statusCode, errorMessage);
        }

        private void ClearFastRestartState()
        {
            _restartPreparationPopupId++;
            _restartPreparationContext?.Invalidate();
            _restartPreparationContext = null;
            _onFastRestartPreparation = null;
            _restartPreparationReady = false;
            _restartPreparationDeadline = 0f;
            _restartPreparationSessionToken = null;
            _fastRestartTraceSessionA = null;
            _isFastRestartInProgress = false;
            _fastRestartRestartSelected = false;
            _fastRestartReplacementSessionReceived = false;
            _fastRestartReplacementConnected = false;
            _fastRestartHandoffDeadline = 0f;
            _bufferedFastRestartReplacementSession = null;
            _onFastRestartCallback = null;
            _onFastRestartFailureCallback = null;
        }

        private void FailSessionReplacement(int statusCode, string errorMessage)
        {
            if (_isSessionReplacementInProgress == false)
            {
                return;
            }

            _isSessionReplacementInProgress = false;
            _sessionReplacementConnectionDeadline = 0f;
            var onFailure = _onSessionReplacementErrorCallback;
            _onSessionReplacementConnectedCallback = null;
            _onSessionReplacementErrorCallback = null;

            FailFastRestart(statusCode, errorMessage);
            InvokeFailureCallback(onFailure, statusCode, errorMessage);
        }

        private static void InvokeFailureCallback(Action<int, string> callback, int statusCode, string errorMessage)
        {
            try
            {
                callback?.Invoke(statusCode, errorMessage);
            }
            catch (Exception exception)
            {
                LoggerHelper.LogError(
                    $"[{nameof(WebSocketService)}][{nameof(InvokeFailureCallback)}] Failure callback threw ({exception.GetType().Name}).");
            }
        }

        private void HandleApplicationClosing()
        {
            if (_isApplicationQuitting)
            {
                return;
            }

            var isSessionHandoff = _isSessionReplacementInProgress || _fastRestartRestartSelected;
            _isApplicationQuitting = true;
            FailFastRestart(1001, "Application closing during Fast Restart.");
            _isSessionReplacementInProgress = false;
            _sessionReplacementConnectionDeadline = 0f;
            ResetTransportGeneration(isSessionHandoff ? SessionReplacementStatusCode : 1001,
                "Application closing.", true, true);
        }

        [ContextMenu("Test Close and Reconnect")]
        private void TestCloseConnectionAndReconnect()
        {
            CloseConnection();

            CoroutineManager.DelayedAction(_timeBeforeReconnect, () => ConnectToServer());
        }

        private void OnSessionOptionsCallback(SessionOptionAction action)
        {
            if (action == SessionOptionAction.Restart && _onFastRestartPreparation != null &&
                _restartPreparationReady == false)
            {
                return;
            }
            // Keep completed combined requests from falling through to a retained legacy callback.
            // The next accepted popup request releases this fence; host actions carry no request identity.
            if (_combinedSessionOptionOwnsActions &&
                (_isFastRestartInProgress == false || _fastRestartRestartSelected))
            {
                return;
            }

            if (action == SessionOptionAction.Restart)
            {
                var initialConnectionStarted = _fastRestartTraceController == null ? "Unknown" :
                    _fastRestartTraceController.InitialConnectionStartedForFastRestartTrace.ToString();
                Debug.Log($"[FastRestartTrace] RestartActionReceived initialConnectionStarted={initialConnectionStarted} " +
                          $"hasBufferedReplacement={_bufferedFastRestartReplacementSession != null} " +
                          $"fastRestartInProgress={_isFastRestartInProgress} replacementInProgress={_isSessionReplacementInProgress} " +
                          $"attemptId={_connectionAttemptId} transportGeneration={_transportGeneration} serviceInstance={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}");
            }

            if (_isFastRestartInProgress)
            {
                if (action != SessionOptionAction.Restart)
                {
                    CompleteFastRestartWithAction(action);
                    return;
                }

                if (_fastRestartRestartSelected)
                {
                    return;
                }

                _fastRestartRestartSelected = true;
                _fastRestartHandoffDeadline = Time.realtimeSinceStartup + FastRestartHandoffTimeoutSeconds;
                BeginFastRestartSessionHandoff();
                ConsumeBufferedFastRestartReplacementSession();
                return;
            }

            _onSessionOptionCallback?.Invoke(action);
        }

        private void OnRestartPreparationRequested(string requestId)
        {
            var popupId = _restartPreparationPopupId;
            // Use the existing dispatcher and fence jobs queued by an earlier popup.
            if (UnityMainThread.Worker == null)
            {
                HandleRestartPreparationRequested(requestId, popupId);
                return;
            }

            UnityMainThread.Worker.AddJob(() => HandleRestartPreparationRequested(requestId, popupId));
        }

        private void HandleRestartPreparationRequested(string requestId, int popupId)
        {
            if (popupId != _restartPreparationPopupId || _onFastRestartPreparation == null ||
                _combinedSessionOptionOwnsActions == false || _isFastRestartInProgress == false ||
                _restartPreparationContext != null || _restartPreparationReady ||
                _fastRestartRestartSelected || _isSessionReplacementInProgress ||
                _fastRestartReplacementSessionReceived || _sessionTransportInvalidated ||
                _isApplicationQuitting || _isConnected == false ||
                string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(SessionToken) ||
                _acceptedRestartPreparationRequests.Contains(requestId))
            {
                return;
            }

            _acceptedRestartPreparationRequests.Add(requestId);
            _restartPreparationTransportGeneration = _transportGeneration;
            _restartPreparationSessionToken = SessionToken;
            // Match the host's existing 10-second preparation window; do not extend command deadlines.
            _restartPreparationDeadline = Time.realtimeSinceStartup + FastRestartHandoffTimeoutSeconds;
            var context = new FastRestartPreparationContext(requestId, CompleteRestartPreparation);
            _restartPreparationContext = context;
            try
            {
                _onFastRestartPreparation.Invoke(context);
            }
            catch (Exception exception)
            {
                FailRestartPreparation(500,
                    $"Fast Restart preparation callback threw ({exception.GetType().Name}).");
            }
        }

        private void CompleteRestartPreparation(FastRestartPreparationContext context, bool succeeded)
        {
            if (ReferenceEquals(context, _restartPreparationContext) == false ||
                _onFastRestartPreparation == null || _isFastRestartInProgress == false ||
                _restartPreparationReady || _fastRestartRestartSelected || _isSessionReplacementInProgress ||
                _isApplicationQuitting)
            {
                return;
            }

            if (succeeded == false)
            {
                FailRestartPreparation(500, "Fast Restart preparation failed.");
                return;
            }

            if (_restartPreparationDeadline <= Time.realtimeSinceStartup)
            {
                FailRestartPreparation(CommandResponseTimeoutStatusCode, "Fast Restart preparation timed out.");
                return;
            }

            if (_isConnected == false || _sessionTransportInvalidated ||
                _restartPreparationTransportGeneration != _transportGeneration ||
                string.Equals(_restartPreparationSessionToken, SessionToken, StringComparison.Ordinal) == false)
            {
                FailRestartPreparation(TransportFailureStatusCode,
                    "Session A is no longer available for Fast Restart preparation.");
                return;
            }

            // Arm the gate before sending because the host may immediately return its Restart action.
            _restartPreparationReady = true;
            _restartPreparationDeadline = 0f;
            try
            {
                _socketLibraryWrapper.SendRestartReady(context.RequestId);
            }
            catch (Exception exception)
            {
                _restartPreparationReady = false;
                FailRestartPreparation(TransportFailureStatusCode,
                    $"Fast Restart preparation acknowledgement could not be sent ({exception.GetType().Name}).");
            }
        }

        private void ProcessRestartPreparationTimeout(float now)
        {
            if (_restartPreparationDeadline > 0f && now >= _restartPreparationDeadline)
            {
                FailRestartPreparation(CommandResponseTimeoutStatusCode, "Fast Restart preparation timed out.");
            }
        }

        private void FailRestartPreparation(int statusCode, string reason)
        {
            if (_onFastRestartPreparation == null || _restartPreparationReady ||
                _isFastRestartInProgress == false)
            {
                return;
            }

            var callback = _onFastRestartFailureCallback;
            var canResumeSessionA = _isSessionReplacementInProgress == false &&
                                    _fastRestartReplacementSessionReceived == false &&
                                    _sessionTransportInvalidated == false && _sessionPayload != null;
            _isSessionARecoveryBlockedAfterFastRestart = canResumeSessionA == false;
            ClearFastRestartState();
            FastRestartRecoveryOwnershipResolved?.Invoke(canResumeSessionA);
            InvokeFailureCallback(callback, statusCode, reason);
        }

        public class SendCommandData
        {
            public CommandRequestType CommandRequestType { get; set; }
            public string CommandRequestJson { get; set; }
            public Action<CommandRequestHandler> OnCommandResponseSuccessHandler { get; set; }
        }

        private sealed class BufferedFastRestartReplacementSession
        {
            public LuxoddSessionPayload SessionPayload { get; }
            public Action OnSessionAcceptedCallback { get; }
            public Action OnConnectedCallback { get; }
            public Action<int, string> OnFailureCallback { get; }

            public BufferedFastRestartReplacementSession(LuxoddSessionPayload sessionPayload,
                Action onSessionAcceptedCallback, Action onConnectedCallback, Action<int, string> onFailureCallback)
            {
                SessionPayload = sessionPayload;
                OnSessionAcceptedCallback = onSessionAcceptedCallback;
                OnConnectedCallback = onConnectedCallback;
                OnFailureCallback = onFailureCallback;
            }
        }
    }
}

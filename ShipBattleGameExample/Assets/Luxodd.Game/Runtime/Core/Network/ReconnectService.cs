using System;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.HelpersAndUtils;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Luxodd.Game.Scripts.Network
{
    public enum ReconnectionState
    {
        None = 0,
        Disconnected,
        InProcess, 
        Connecting,
        Connected,
        NotConnected,
        ConnectingFailed,
    }

    public class ReconnectService : MonoBehaviour
    {
        public ISimpleEvent<ReconnectionState> ReconnectStateChangedEvent => _onReconnectionStateChangedEvent;
        
        [FormerlySerializedAs("_webSocketHandler")] [SerializeField] private WebSocketLibraryWrapper _webSocketLibraryWrapper;
        [SerializeField] private WebSocketService _webSocketService;
        [SerializeField] private ErrorHandlerService _errorHandlerService;
        [SerializeField] private int _maxReconnectAttempts = 3;
        
        [SerializeField] private float _delayBetweenReconnections = 0.5f;
        
        private ReconnectionState _reconnectionState = ReconnectionState.None;
        private int _currentReconnectAttempt = 0;
        private bool _reconnectDeferredDuringFastRestart;
        private bool _deferredReconnectAttemptAlreadyCounted;
        private bool _deferredReconnectAttemptsExhausted;
        
        private readonly SimpleEvent<ReconnectionState> _onReconnectionStateChangedEvent = new SimpleEvent<ReconnectionState>();
        
        private void Awake()
        {
            _webSocketLibraryWrapper.WebSocketClosedEvent.AddListener(OnWebSocketConnectionClosedHandler);
            _webSocketService.FastRestartRecoveryOwnershipResolved += OnFastRestartRecoveryOwnershipResolved;
        }

        private void OnDestroy()
        {
            if (_webSocketLibraryWrapper != null)
            {
                _webSocketLibraryWrapper.WebSocketClosedEvent.RemoveListener(OnWebSocketConnectionClosedHandler);
            }

            if (_webSocketService != null)
            {
                _webSocketService.FastRestartRecoveryOwnershipResolved -= OnFastRestartRecoveryOwnershipResolved;
            }
        }

        private void SubscribeToEvents()
        {
            _webSocketLibraryWrapper.WebSocketOpenedEvent.AddListener(OnWebSocketConnectedHandler);
        }

        private void UnsubscribeFromEvents()
        {
            _webSocketLibraryWrapper.WebSocketOpenedEvent.RemoveListener(OnWebSocketConnectedHandler);
        }

        private void OnWebSocketConnectionClosedHandler(int code)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketConnectionClosedHandler)}] OK, code: {code}");

            if (ShouldDiscardSessionARecovery())
            {
                ClearDeferredReconnect();
                return;
            }

            if (_webSocketService.IsFastRestartInProgress)
            {
                DeferReconnectDuringFastRestart(false, _currentReconnectAttempt >= _maxReconnectAttempts);
                return;
            }

            if (_webSocketService.IsConnectionAttemptInProgress || _webSocketService.IsConnected)
            {
                return;
            }
            
            if (_currentReconnectAttempt >= _maxReconnectAttempts)
            {
                SwitchToState(ReconnectionState.ConnectingFailed);
            }
            else
            {
                if (_currentReconnectAttempt == 0)
                {
                    _onReconnectionStateChangedEvent.Notify(ReconnectionState.Connecting);
                }
                SwitchToState(ReconnectionState.Disconnected);
            }
        }

        private void OnWebSocketConnectedHandler()
        {
            _webSocketLibraryWrapper.WebSocketOpenedEvent.RemoveListener(OnWebSocketConnectedHandler);
            ClearDeferredReconnect();
            SwitchToState(ReconnectionState.Connected);
        }

        private void OnWebSocketConnectionFailedHandler(string errorMessage)
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnWebSocketConnectionFailedHandler)}] WebSocket connection failed.");
            SwitchToState(ReconnectionState.NotConnected);
        }

        private void ResetAttempts()
        {
            _currentReconnectAttempt = 0;
        }

        private void UpdateState()
        {
            switch (_reconnectionState)
            {
                case ReconnectionState.None:
                    break;
                case ReconnectionState.Disconnected:
                    OnDisconnected();
                    break;
                case ReconnectionState.InProcess:
                    OnInProcess();
                    break;
                case ReconnectionState.Connecting:
                    OnConnecting();
                    break;
                case ReconnectionState.Connected:
                    OnConnected();
                    break;
                case ReconnectionState.NotConnected:
                    OnNotConnected();
                    break;
                case ReconnectionState.ConnectingFailed:
                    OnConnectingFailed();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDisconnected()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDisconnected)}] OK");
            
            UnsubscribeFromEvents();
            
            SubscribeToEvents();

            _currentReconnectAttempt++;
            
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDisconnected)}] OK, attempt: {_currentReconnectAttempt}");
            
            CoroutineManager.DelayedAction(_delayBetweenReconnections, () =>
            {
                LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnDisconnected)}] OK, start to connect");

                if (ShouldDiscardSessionARecovery())
                {
                    ClearDeferredReconnect();
                    SwitchToState(ReconnectionState.None);
                    return;
                }

                if (_webSocketService.IsFastRestartInProgress)
                {
                    DeferReconnectDuringFastRestart(true, false);
                    SwitchToState(ReconnectionState.None);
                    return;
                }

                if (_webSocketService.IsConnected)
                {
                    SwitchToState(ReconnectionState.Connected);
                    return;
                }

                if (_webSocketService.IsConnectionAttemptInProgress)
                {
                    SwitchToState(ReconnectionState.Connecting);
                    return;
                }

                SwitchToState(ReconnectionState.InProcess);    
            });
        }

        private void OnInProcess()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnInProcess)}] OK");
            if (ShouldDiscardSessionARecovery())
            {
                ClearDeferredReconnect();
                SwitchToState(ReconnectionState.None);
                return;
            }

            if (_webSocketService.IsFastRestartInProgress)
            {
                DeferReconnectDuringFastRestart(true, false);
                SwitchToState(ReconnectionState.None);
                return;
            }

            _webSocketService.TryReconnectFromReconnectService();
            SwitchToState(ReconnectionState.Connecting);
        }

        private void OnFastRestartRecoveryOwnershipResolved(bool sessionAStillValid)
        {
            if (sessionAStillValid == false)
            {
                ClearDeferredReconnect();
                ResetAttempts();
                if (_reconnectionState != ReconnectionState.None)
                {
                    SwitchToState(ReconnectionState.None);
                }

                return;
            }

            if (_reconnectDeferredDuringFastRestart == false)
            {
                return;
            }

            var canResumeSessionA = ShouldDiscardSessionARecovery() == false &&
                                    _webSocketService.IsConnected == false &&
                                    _webSocketService.IsConnectionAttemptInProgress == false;
            var attemptAlreadyCounted = _deferredReconnectAttemptAlreadyCounted;
            var attemptsExhausted = _deferredReconnectAttemptsExhausted;
            ClearDeferredReconnect();

            if (canResumeSessionA == false)
            {
                return;
            }

            if (attemptsExhausted)
            {
                SwitchToState(ReconnectionState.ConnectingFailed);
            }
            else if (attemptAlreadyCounted)
            {
                SwitchToState(ReconnectionState.InProcess);
            }
            else
            {
                SwitchToState(ReconnectionState.Disconnected);
            }
        }

        private bool ShouldDiscardSessionARecovery()
        {
            return _webSocketService.IsSessionReplacementInProgress ||
                   _webSocketService.IsSessionARecoveryBlockedAfterFastRestart;
        }

        private void DeferReconnectDuringFastRestart(bool attemptAlreadyCounted, bool attemptsExhausted)
        {
            _reconnectDeferredDuringFastRestart = true;
            _deferredReconnectAttemptAlreadyCounted |= attemptAlreadyCounted;
            _deferredReconnectAttemptsExhausted |= attemptsExhausted;
        }

        private void ClearDeferredReconnect()
        {
            _reconnectDeferredDuringFastRestart = false;
            _deferredReconnectAttemptAlreadyCounted = false;
            _deferredReconnectAttemptsExhausted = false;
        }

        private void OnConnecting()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnConnecting)}] OK");
        }

        private void OnConnected()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnConnected)}] OK");
            
            _onReconnectionStateChangedEvent.Notify(ReconnectionState.Connected);
            ResetAttempts();
            SwitchToState(ReconnectionState.None);
        }

        private void OnNotConnected()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnNotConnected)}] OK");
            _currentReconnectAttempt++;
            if (_currentReconnectAttempt >= _maxReconnectAttempts)
            {
                SwitchToState(ReconnectionState.ConnectingFailed);
            }
            else
            {
                SwitchToState(ReconnectionState.InProcess);
            }
        }

        private void OnConnectingFailed()
        {
            LoggerHelper.Log($"[{DateTime.Now}][{GetType().Name}][{nameof(OnConnectingFailed)}] OK");
            ResetAttempts();
            
            _onReconnectionStateChangedEvent.Notify(ReconnectionState.ConnectingFailed);
            SwitchToState(ReconnectionState.None);
            _errorHandlerService.HandleConnectionError("Connection Lost");
        }

        private void SwitchToState(ReconnectionState state)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(SwitchToState)}] OK, was {_reconnectionState}, new state: {state}");
            _reconnectionState = state;
            UpdateState();
        }
    }
}

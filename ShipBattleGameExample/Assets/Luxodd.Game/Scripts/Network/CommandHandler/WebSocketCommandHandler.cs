using System;
using System.Collections.Generic;
using System.Linq;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Luxodd.Game.Scripts.Network.CommandHandler
{

    public enum CommandProcessState
    {
        None,
        Sent,
        Received,
    }

    public class WebSocketCommandHandler : MonoBehaviour
    {
        public ISimpleEvent<CommandProcessState> OnCommandProcessStateChangeEvent => _commandProcessStateChangeEvent;

        [FormerlySerializedAs("_websocketService")] [SerializeField]
        private WebSocketService _webSocketService;

        [SerializeField] private ErrorHandlerService _errorHandlerService;

        private Dictionary<CommandRequestType, BaseCommandHandler> _commandHandlers =
            new Dictionary<CommandRequestType, BaseCommandHandler>();

        private SimpleEvent<CommandProcessState> _commandProcessStateChangeEvent =
            new SimpleEvent<CommandProcessState>();

        public void SendProfileRequestCommand(Action<string> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            var commandHandler = _commandHandlers[CommandRequestType.GetProfileRequest];
            commandHandler.SendCommand(() =>
                OnProfileResponseHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendUserBalanceRequestCommand(Action<int> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = _commandHandlers[CommandRequestType.GetUserBalanceRequest];
            commandHandler.SendCommand(() =>
                OnUserBalanceRequestHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendAddBalanceRequestCommand(int amount, int pinCode, Action onSuccess,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);
            var commandHandler = _commandHandlers[CommandRequestType.AddBalanceRequest];
            commandHandler.SendCommand(
                () => OnAddBalanceRequestSuccessHandler(commandHandler, onSuccess, onFailureCallback), amount, pinCode);
        }

        public void SendChargeUserBalanceRequestCommand(int amount, int pinCode, Action onSuccess, Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);
            var commandHandler = _commandHandlers[CommandRequestType.ChargeUserBalanceRequest];
            commandHandler.SendCommand(
                () => OnChargeUserBalanceRequestSuccessHandler(commandHandler, onSuccess, onFailureCallback), amount, pinCode);
        }

        public void SendHealthCheckStatusCommand(Action onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;
            
            var commandHandler = _commandHandlers[CommandRequestType.HealthStatusCheckRequest];
            commandHandler.SendCommand(() =>
                OnHealthCheckStatusSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendLeaderboardRequestCommand(Action<LeaderboardDataResponse> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = _commandHandlers[CommandRequestType.LeaderboardRequest];
            commandHandler.SendCommand(() =>
                OnLeaderboardRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendLevelBeginRequestCommand(int level, Action onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = _commandHandlers[CommandRequestType.LevelBeginRequest];
            commandHandler.SendCommand(
                () => OnLevelBeginRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback), level);
        }

        public void SendLevelEndRequestCommand(int level, int score, Action onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = _commandHandlers[CommandRequestType.LevelEndRequest];
            commandHandler.SendCommand(
                () => OnLevelEndRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback), level,
                score);
        }

        public void SendGetUserDataRequestCommand(Action<object> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;
            
            var commandHandler = _commandHandlers[CommandRequestType.GetUserDataRequest];
            commandHandler.SendCommand(() =>
                OnGetUserDataRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendSetUserDataRequestCommand(object userData, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;
            
            var commandHandler = _commandHandlers[CommandRequestType.SetUserDataRequest];
            commandHandler.SendCommand(
                () => OnSendSetUserDataRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback),
                userData);
        }

        private bool CheckConnectionStatus(Action<int, string> onFailureCallback)
        {
            if (_webSocketService.IsConnected) return true;
            const int connectionError = 1006;
            const string message = "The websocket is not connected.";
            onFailureCallback?.Invoke(connectionError, message);
            _errorHandlerService.HandleConnectionError($"Health status request failed, error: {message}");
            return false;
        }

        private void Awake()
        {
            PrepareCommands();
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(Awake)}] OK, total commands: {_commandHandlers.Count}");
        }

        private void OnProfileResponseHandler(BaseCommandHandler handler, Action<string> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnProfileResponseHandler)}] OK, status: {handler.ResponseStatus}");
            
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleGameError($"Get user profile request failed, error: {handler.ErrorMessage}");
                return;
            }

            var profilePayload = (ProfilePayload)handler.ResponseHandler.Payload;
            if (profilePayload != null)
            {
                onSuccessCallback?.Invoke(profilePayload.Name);
            }
        }

        private void OnUserBalanceRequestHandler(BaseCommandHandler handler, Action<int> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnUserBalanceRequestHandler)}] OK, status: {handler.ResponseStatus}");

            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleCreditsError(
                    $"Get user balance request failed, error: {handler.ErrorMessage}");
                return;
            }

            var userBalancePayload = (UserBalancePayload)handler.ResponseHandler.Payload;
            if (userBalancePayload == null) return;
            onSuccessCallback?.Invoke(userBalancePayload.Balance);
        }

        private void OnAddBalanceRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Received);
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnChargeUserBalanceRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnChargeUserBalanceRequestSuccessHandler)}] OK, status: {handler.ResponseStatus}");

            _commandProcessStateChangeEvent.Notify(CommandProcessState.Received);
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnHealthCheckStatusSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnLeaderboardRequestSuccessHandler(BaseCommandHandler handler,
            Action<LeaderboardDataResponse> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                return;
            }

            var leaderboardDataResponse = (LeaderboardDataResponse)handler.ResponseHandler.Payload;
            onSuccessCallback?.Invoke(leaderboardDataResponse);

        }

        private void OnLevelBeginRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleConnectionError(
                    $"Send level begin request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnLevelEndRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleConnectionError(
                    $"Send level end request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnGetUserDataRequestSuccessHandler(BaseCommandHandler handler, Action<object> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleConnectionError(
                    $"Send get user data request failed, error: {handler.ErrorMessage}");
                return;
            }
            
            onSuccessCallback?.Invoke(handler.ResponseHandler.Payload);
        }

        private void OnSendSetUserDataRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                _errorHandlerService.HandleConnectionError(
                    $"Send set user data request failed, error: {handler.ErrorMessage}");
                return;
            }
            onSuccessCallback?.Invoke();
        }

        private void PrepareCommands()
        {
            var commandTypes = Enum.GetValues(CommandRequestType.GetProfileRequest.GetType())
                .Cast<CommandRequestType>()
                .ToList();

            //TODO: remove after implementation
            commandTypes.Remove(CommandRequestType.GameStatsRequest);
            commandTypes.Remove(CommandRequestType.GetUserBestScoreRequest);
            commandTypes.Remove(CommandRequestType.GetUserRecentGamesRequest);

            foreach (var commandType in commandTypes)
            {
                _commandHandlers[commandType] = BaseCommandHandler.Build(_webSocketService, commandType);
            }
        }
    }
}
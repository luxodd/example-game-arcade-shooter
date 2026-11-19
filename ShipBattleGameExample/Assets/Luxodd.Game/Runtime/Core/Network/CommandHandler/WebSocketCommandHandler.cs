using System;
using System.Collections.Generic;
using Luxodd.Game.HelpersAndUtils.Utils;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Luxodd.Game.Scripts.HelpersAndUtils.Logger;
using Luxodd.Game.Scripts.Missions;
using Luxodd.Game.Scripts.Network.Payloads;
using UnityEngine;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

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

        [SerializeField] private WebSocketService _webSocketService;

        [SerializeField] private ErrorHandlerService _errorHandlerService;

        private SimpleEvent<CommandProcessState> _commandProcessStateChangeEvent =
            new SimpleEvent<CommandProcessState>();

        internal WebSocketService SocketService => _webSocketService;

        public void SendScoreUpdateRequestCommand(int requestId, int level, int score,
            Action<ScoreUpdateResponse> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (requestId < 0 || level < 0 || score < 0)
            {
                onFailureCallback?.Invoke(400, "Score update request_id, level and score must be non-negative.");
                return;
            }
#if NEWTONSOFT_JSON
            var request = new CommandRequestJson
            {
                Type = "score_update",
                Version = "1.0",
                Payload = new ScoreUpdatePayload { RequestId = requestId, Level = level, Score = score }
            };
            _webSocketService.SendScoreUpdateCommand(requestId, JsonConvert.SerializeObject(request), handler =>
            {
                if (handler.CommandResponse.StatusCode == 200)
                    onSuccessCallback?.Invoke((ScoreUpdateResponse)handler.CommandResponse.Payload);
                else
                    onFailureCallback?.Invoke(handler.CommandResponse.StatusCode, handler.ErrorMessage);
            });
#else
            onFailureCallback?.Invoke(400, "Score updates require the Newtonsoft JSON package.");
#endif
        }

        public void SendProfileRequestCommand(Action<string> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.GetProfileRequest);
            commandHandler.SendCommand(() =>
                OnProfileResponseHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendUserBalanceRequestCommand(Action<float> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            //if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.GetUserBalanceRequest);
            commandHandler.SendCommand(() =>
                OnUserBalanceRequestHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        [Obsolete("Direct credit operations are no longer recommended. Credit and payment operations are managed by the Luxodd platform.", false)]
        public void SendAddBalanceRequestCommand(int amount, int pinCode, Action onSuccess,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);
            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.AddBalanceRequest);
            commandHandler.SendCommand(
                () => OnAddBalanceRequestSuccessHandler(commandHandler, onSuccess, onFailureCallback), amount, pinCode);
        }

        [Obsolete("Direct credit operations are no longer recommended. Credit and payment operations are managed by the Luxodd platform.", false)]
        public void SendChargeUserBalanceRequestCommand(int amount, int pinCode, Action onSuccess,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);
            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.ChargeUserBalanceRequest);
            commandHandler.SendCommand(
                () => OnChargeUserBalanceRequestSuccessHandler(commandHandler, onSuccess, onFailureCallback), amount,
                pinCode);
        }

        public void SendGetMerchantPrizeInfoRequestCommand(Action<MerchantPrizeInfoResponse> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);

            var commandHandler = BaseCommandHandler.Build(_webSocketService,
                CommandRequestType.GetMerchantPrizeInfoRequest);
            commandHandler.SendCommand(() =>
                OnSendGetMerchantPrizeInfoRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendHealthCheckStatusCommand(Action onSuccessCallback, Action<int, string> onFailureCallback)
        {
            //if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.HealthStatusCheckRequest);
            commandHandler.SendCommand(() =>
                OnHealthCheckStatusSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendLeaderboardRequestCommand(Action<LeaderboardDataResponse> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            //if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.LeaderboardRequest);
            commandHandler.SendCommand(() =>
                OnLeaderboardRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendLeaderboardAroundScoreRequestCommand(int score,
            Action<LeaderboardAroundScoreDataResponse> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            var commandHandler = BaseCommandHandler.Build(_webSocketService,
                CommandRequestType.LeaderboardAroundScoreRequest);
            commandHandler.SendCommand(() =>
                OnLeaderboardAroundScoreRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback),
                score);
        }

        public void SendLevelBeginRequestCommand(int level, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.LevelBeginRequest);
            commandHandler.SendCommand(
                () => OnLevelBeginRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback), level);
        }

        public void SendLevelEndRequestCommand(int level, int score, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.LevelEndRequest);
            commandHandler.SendCommand(
                () => OnLevelEndRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback), level,
                score);
        }

        public void SendGetUserDataRequestCommand(Action<object> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            //if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.GetUserDataRequest);
            commandHandler.SendCommand(() =>
                OnGetUserDataRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendSetUserDataRequestCommand(object userData, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            //if (CheckConnectionStatus(onFailureCallback) == false) return;

            var commandHandler = BaseCommandHandler.Build(_webSocketService, CommandRequestType.SetUserDataRequest);
            commandHandler.SendCommand(
                () => OnSendSetUserDataRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback),
                userData);
        }

        public void SendGetGameSessionInfoRequestCommand(Action<SessionInfoPayload> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);

            var commandHandler = BaseCommandHandler.Build(_webSocketService,
                CommandRequestType.GetGameSessionInfoRequest);
            commandHandler.SendCommand(() =>
                OnSendGetGameSessionInfoRequestSuccessHandler(commandHandler, onSuccessCallback, onFailureCallback));
        }

        public void SendGetBettingSessionMissionsRequestCommand(Action<BettingSessionMissionsPayload> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Sent);

            var commandHandler = BaseCommandHandler.Build(_webSocketService,
                CommandRequestType.GetBettingSessionMissionsRequest);
            commandHandler.SendCommand(() =>
                OnSendGetBettingSessionMissionsRequestSuccessHandler(commandHandler, onSuccessCallback,
                    onFailureCallback));
        }

        public void SendStrategicBettingResultRequest(List<MissionResultDto> missionResultList,
            Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            var commandHandler = BaseCommandHandler.Build(_webSocketService,
                CommandRequestType.SendStrategicBettingResultRequest);
            commandHandler.SendCommand(
                () => OnSendStrategicBettingResultRequestSuccessHandler(commandHandler, onSuccessCallback,
                    onFailureCallback), missionResultList);
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

        private void OnProfileResponseHandler(BaseCommandHandler handler, Action<string> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnProfileResponseHandler)}] OK, status: {handler.ResponseStatus}");

            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
                _errorHandlerService.HandleGameError($"Get user profile request failed, error: {handler.ErrorMessage}");
                return;
            }

            var profilePayload = (ProfilePayload)handler.ResponseHandler.Payload;
            if (profilePayload != null)
            {
                onSuccessCallback?.Invoke(profilePayload.Name);
            }
        }

        private void OnUserBalanceRequestHandler(BaseCommandHandler handler, Action<float> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            LoggerHelper.Log(
                $"[{DateTime.Now}][{GetType().Name}][{nameof(OnUserBalanceRequestHandler)}] OK, status: {handler.ResponseStatus}");

            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
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

        private void OnSendGetMerchantPrizeInfoRequestSuccessHandler(BaseCommandHandler handler,
            Action<MerchantPrizeInfoResponse> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Received);

            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
                _errorHandlerService.HandleGameError(
                    $"Send get merchant prize info request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke((MerchantPrizeInfoResponse)handler.ResponseHandler.Payload);
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

        private void OnLeaderboardAroundScoreRequestSuccessHandler(BaseCommandHandler handler,
            Action<LeaderboardAroundScoreDataResponse> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                return;
            }

            var leaderboardDataResponse = (LeaderboardAroundScoreDataResponse)handler.ResponseHandler.Payload;
            onSuccessCallback?.Invoke(leaderboardDataResponse);
        }

        private void OnLevelBeginRequestSuccessHandler(BaseCommandHandler handler, Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
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
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
                _errorHandlerService.HandleConnectionError(
                    $"Send level end request failed, error: {handler.ErrorMessage}");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR && NEWTONSOFT_JSON
            var levelEndPayload = handler.ResponseHandler.Payload as LevelEndResponsePayload;
            if (levelEndPayload?.PrizeTickets != null && levelEndPayload.PrizeTickets.Count > 0)
            {
                var prizeTicketsJson = JsonConvert.SerializeObject(levelEndPayload.PrizeTickets);
                LoggerHelper.Log(
                    $"[{DateTime.Now}][{GetType().Name}][{nameof(OnLevelEndRequestSuccessHandler)}] " +
                    $"OK, event=prize_won, ticketCount={levelEndPayload.PrizeTickets.Count}, forwarding=WebGL");
                _webSocketService.NotifyPrizeWon(prizeTicketsJson);
            }
#endif

            onSuccessCallback?.Invoke();
        }

        private void OnGetUserDataRequestSuccessHandler(BaseCommandHandler handler, Action<object> onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
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
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
                _errorHandlerService.HandleGameError(
                    $"Send set user data request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke();
        }

        private void OnSendGetGameSessionInfoRequestSuccessHandler(BaseCommandHandler handler,
            Action<SessionInfoPayload> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Received);
            
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode || handler.StatusCode == 408) return;
                _errorHandlerService.HandleGameError(
                    $"Send get game session info request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke((SessionInfoPayload)handler.ResponseHandler.Payload);
        }

        private void OnSendGetBettingSessionMissionsRequestSuccessHandler(BaseCommandHandler handler,
            Action<BettingSessionMissionsPayload> onSuccessCallback, Action<int, string> onFailureCallback)
        {
            _commandProcessStateChangeEvent.Notify(CommandProcessState.Received);
            
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode || handler.StatusCode == 408) return;
                _errorHandlerService.HandleGameError(
                    $"Send get game session info request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke((BettingSessionMissionsPayload)handler.ResponseHandler.Payload);
        }

        private void OnSendStrategicBettingResultRequestSuccessHandler(BaseCommandHandler handler,
            Action onSuccessCallback,
            Action<int, string> onFailureCallback)
        {
            if (handler.ResponseStatus != CommandResponseStatus.Ok)
            {
                onFailureCallback?.Invoke(handler.StatusCode, handler.ErrorMessage);
                if (handler.StatusCode == WebSocketService.SessionReplacementStatusCode) return;
                _errorHandlerService.HandleGameError(
                    $"Send strategic betting result request failed, error: {handler.ErrorMessage}");
                return;
            }

            onSuccessCallback?.Invoke();
        }

    }
}

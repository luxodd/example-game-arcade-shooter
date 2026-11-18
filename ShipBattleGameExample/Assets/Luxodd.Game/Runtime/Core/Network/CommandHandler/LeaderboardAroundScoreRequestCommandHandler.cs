using System;
using Luxodd.Game.Scripts.Game.Leaderboard;
using Luxodd.Game.Scripts.Network.Payloads;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Network.CommandHandler
{
    public class LeaderboardAroundScoreRequestCommandHandler : BaseCommandHandler
    {
        public LeaderboardAroundScoreRequestCommandHandler(WebSocketService webSocketService) : base(webSocketService)
        {
        }

        public override void SendCommand(Action onCommandComplete, params object[] parameters)
        {
            _onCommandCompletedCallback = onCommandComplete;

            var targetScore = Convert.ToInt32(parameters[0]);
            var commandRequest = new CommandRequestJson
            {
                MessageVersion = "1",
                Type = "leaderboard_around_score_request",
                Payload = new LeaderboardAroundScoreRequestPayload
                {
                    TargetScore = targetScore
                }
            };

#if NEWTONSOFT_JSON
            var commandRequestJson = JsonConvert.SerializeObject(commandRequest);
            WebSocketService.SendCommand(CommandRequestType.LeaderboardAroundScoreRequest, commandRequestJson,
                OnCommandResponseSuccessHandler);
#endif

            SendStatus = CommandSendStatus.Pending;
        }

        protected override void OnCommandResponseSuccessHandler(CommandRequestHandler responseHandler)
        {
            base.OnCommandResponseSuccessHandler(responseHandler);

#if NEWTONSOFT_JSON
            var payloadJson = JsonConvert.SerializeObject(ResponseHandler.Payload);
            var payloadObject = JsonConvert.DeserializeObject<LeaderboardAroundScoreDataResponse>(payloadJson);
            ResponseHandler.Payload = payloadObject;
#endif

            _onCommandCompletedCallback?.Invoke();
        }
    }
}

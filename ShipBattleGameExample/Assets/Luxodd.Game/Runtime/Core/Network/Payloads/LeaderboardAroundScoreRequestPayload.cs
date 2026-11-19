#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Network.Payloads
{
    public class LeaderboardAroundScoreRequestPayload
    {
#if NEWTONSOFT_JSON
        [JsonProperty("target_score")] public int TargetScore { get; set; }
#else
        public int TargetScore { get; set; }
#endif
    }
}

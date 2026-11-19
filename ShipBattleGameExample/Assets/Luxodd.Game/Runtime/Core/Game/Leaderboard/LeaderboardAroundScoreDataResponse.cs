using System.Collections.Generic;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Game.Leaderboard
{
    public class LeaderboardAroundScoreDataResponse
    {
#if NEWTONSOFT_JSON
        [JsonProperty("leaderboard")] public List<LeaderboardData> Leaderboard { get; set; }
#else
        public List<LeaderboardData> Leaderboard { get; set; }
#endif
    }
}

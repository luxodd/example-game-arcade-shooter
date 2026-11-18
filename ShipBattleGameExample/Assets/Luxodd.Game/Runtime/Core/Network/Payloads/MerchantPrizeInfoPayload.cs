#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Network.Payloads
{
    public class MerchantPrizeInfoPayload
    {
#if NEWTONSOFT_JSON
        [JsonProperty("campaign_id")] public string CampaignId { get; set; }
        [JsonProperty("prize_name")] public string PrizeName { get; set; }
        [JsonProperty("prize_image_url")] public string PrizeImageUrl { get; set; }
        [JsonProperty("prize_description")] public string PrizeDescription { get; set; }
        [JsonProperty("goal_score")] public int GoalScore { get; set; }
        [JsonProperty("dynamic_hardness")] public int DynamicHardness { get; set; }
        [JsonProperty("target_dwell_seconds")] public float TargetDwellSeconds { get; set; }
        [JsonProperty("points_per_second")] public float PointsPerSecond { get; set; }
        [JsonProperty("player_name")] public string PlayerName { get; set; }
#else
        public string CampaignId { get; set; }
        public string PrizeName { get; set; }
        public string PrizeImageUrl { get; set; }
        public string PrizeDescription { get; set; }
        public int GoalScore { get; set; }
        public int DynamicHardness { get; set; }
        public float TargetDwellSeconds { get; set; }
        public float PointsPerSecond { get; set; }
        public string PlayerName { get; set; }
#endif
    }
}

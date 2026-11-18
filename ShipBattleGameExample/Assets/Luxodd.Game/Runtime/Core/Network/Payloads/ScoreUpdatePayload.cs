namespace Luxodd.Game.Scripts.Network.Payloads
{
    public sealed class ScoreUpdatePayload
    {
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("request_id")]
#endif
        public int RequestId { get; set; }
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("score")]
#endif
        public int Score { get; set; }
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("level")]
#endif
        public int Level { get; set; }
    }
}

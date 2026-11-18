namespace Luxodd.Game.Scripts.Network.Payloads
{
    public sealed class ScoreUpdateResponse
    {
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("request_id", Required = Newtonsoft.Json.Required.Always)]
#endif
        public int RequestId { get; set; }
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("result")]
#endif
        public string Result { get; set; }
#if NEWTONSOFT_JSON
        [Newtonsoft.Json.JsonProperty("applied")]
#endif
        public bool Applied { get; set; }
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

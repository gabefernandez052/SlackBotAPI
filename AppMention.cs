using Newtonsoft.Json;

namespace SlackBotAPI
{
    public class AppMention
    {
        [JsonProperty(PropertyName = "event")]
        public EventInfo @Event { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        public class EventInfo
        {
            [JsonProperty(PropertyName = "type")]
            public string type { get; set; }

            [JsonProperty(PropertyName = "user")]
            public string user { get; set; }

            [JsonProperty(PropertyName = "text")]
            public string Text { get; set; }

            [JsonProperty(PropertyName = "channel")]
            public string Channel { get; set; }
        }
    }
}
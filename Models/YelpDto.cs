using Newtonsoft.Json;

namespace SlackBotAPI.Models
{
    public class YelpDto
    {
        [JsonProperty(PropertyName = "businesses")]
        public Business[] lstBusinesses { get; set; }

        public class Business
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "location")]
            public LocationInfo Location { get; set; }

            [JsonProperty(PropertyName = "url")]
            public string Url { get; set; }
        }

        public class LocationInfo
        {
            [JsonProperty(PropertyName = "address1")]
            public string Address1 { get; set; }

            [JsonProperty(PropertyName = "address2")]
            public string Address2 { get; set; }

            [JsonProperty(PropertyName = "address3")]
            public string Address3 { get; set; }

            [JsonProperty(PropertyName = "city")]
            public string City { get; set; }

            [JsonProperty(PropertyName = "zip_code")]
            public string Zip_code { get; set; }

            [JsonProperty(PropertyName = "state")]
            public string State { get; set; }
        }
    }
}
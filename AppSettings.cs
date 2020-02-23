using System.Collections.Generic;

namespace SlackBotAPI
{
    public class AppSettings
    {
        public YelpParameters YelpParams { get; set; }

        public class YelpParameters
        {
            public string SearchTerm { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string Radius { get; set; }
            public string Price { get; set; }
            public string Limit { get; set; }
        }
    }
}
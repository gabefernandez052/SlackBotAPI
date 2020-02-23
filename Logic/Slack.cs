using Newtonsoft.Json;
using SlackBotAPI.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SlackBotAPI.Logic
{
    public class Slack
    {
        private readonly AppSettings _appSettings;
        private readonly int _randomSelector;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="appSettings"></param>
        public Slack(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _randomSelector = new Random().Next(0, 25);
        }

        /// <summary>
        /// Check if user request prompts a yelp search
        /// </summary>
        /// <param name="requestText"></param>
        /// <returns></returns>
        public bool ValidRequest(string requestText)
        {
            return requestText.Contains("Happy Hour", StringComparison.OrdinalIgnoreCase) ||
                   requestText.Contains("try again", StringComparison.OrdinalIgnoreCase) ||
                   requestText.Contains("another one", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get from Yelp.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetHappyHourSuggestions(string apiKey)
        {
            YelpDto dtoResponse = null;
            using var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var endPoint = $"https://api.yelp.com/v3/businesses/search" +
                    $"?term={_appSettings.YelpParams.SearchTerm}" +
                    $"&latitude={_appSettings.YelpParams.Latitude}" +
                    $"&longitude={_appSettings.YelpParams.Longitude}" +
                    $"&radius={_appSettings.YelpParams.Radius}" +
                    $"&price={_appSettings.YelpParams.Price}" +
                    $"&limit={_appSettings.YelpParams.Limit}";

                await client.GetAsync(endPoint).ContinueWith((taskResponse) =>
                {
                    var jsonString = taskResponse.Result.Content.ReadAsStringAsync();
                    jsonString.Wait();
                    dtoResponse = JsonConvert.DeserializeObject<YelpDto>(jsonString.Result);
                });
            }
            catch (Exception e)
            {
                //TODO - Add exception handling
            }
            finally
            {
                client?.Dispose();
            }

            var response = dtoResponse?.lstBusinesses[_randomSelector];

            return $"How about {response.Name}. It's located at {response.Location.Address1} " +
                $"{response.Location.City},{response.Location.State}. " +
                $" Click the link for more details -> " +
                $"<{response.Url}|{response.Name}>";
        }
    }
}
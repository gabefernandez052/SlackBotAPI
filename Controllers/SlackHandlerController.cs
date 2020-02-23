using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SlackBotAPI.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Generic;

namespace SlackBotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SlackHandlerController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly int _randomSelector;

        private readonly Dictionary<string,string> _secrets;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="appSettings"></param>
        public SlackHandlerController(IOptions<AppSettings> appSettings)

        {
            _appSettings = appSettings.Value;

            _secrets = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(
               Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "secret.json")));

            _randomSelector = new Random().Next(0, 25);
        }

        /// <summary>
        /// Entry point when bot mentioned
        /// </summary>
        /// <param name="mention"></param>
        [HttpPost]
        [Route("SlackBot")]
        public async void SlackBot(AppMention mention)
        {
            using var client = new HttpClient();

            try
            {
                var response = new
                {
                    text = ValidRequest(mention.Event.Text)
                    ? await GetHappyHourSuggestions()
                    : $"Sorry, <@{mention.Event.User}>. I don't know what you mean. Please ask me about happy hour"
                };

                await client.PostAsync(_secrets["SlackBotEndPoint"],
                        new StringContent(JsonConvert.SerializeObject(response), System.Text.Encoding.UTF8, "application/json"));


            }
            catch (Exception e)
            {
                //TODO - Add exception handling
            }
            finally
            {
                client?.Dispose();
            }
        }

        /// <summary>
        /// Check if user request prompts a yelp search
        /// </summary>
        /// <param name="requestText"></param>
        /// <returns></returns>
        private bool ValidRequest(string requestText)
        {
            return requestText.Contains("Happy Hour", StringComparison.OrdinalIgnoreCase) ||
                   requestText.Contains("try again", StringComparison.OrdinalIgnoreCase) ||
                   requestText.Contains("another one", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get from Yelp.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetHappyHourSuggestions()
        {
            YelpDto dtoResponse = null;
            using var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secrets["yelpAPIKey"]);

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

        [HttpPost]
        [Route("hashtag")]
        public async Task<string> Hashtag(AppMention mention)
        {
            try
            {
                var baseUrl = "https://api.ritekit.com/v1/stats/auto-hashtag";
                var client = new HttpClient();
                
                var query = HttpUtility.ParseQueryString(string.Empty);
                query["client_id"] = _secrets["ritekitAPIKey"];
                query["maxHashtags"] = "5";
                query["hashtagPosition"] = "auto";
                query["post"] = mention.Event.Text;
                string queryString = query.ToString();

                string getUrl = baseUrl + "?" + queryString;
                Console.WriteLine(getUrl);
                
                // POST Example
                // var values = new Dictionary<string, string>{
                //     { "client_id", _secrets["ritekitAPIKey"] },
                //     { "maxHashtags", "5" },
                //     { "hashtagPosition", "auto" },
                //     { "post", mention.Event.Text },
                // };

                // var content = new FormUrlEncodedContent(values);
                // var response = await client.PostAsync("https://api.ritekit.com/v1/stats/auto-hashtag", content);
                
                var response = await client.GetAsync(getUrl);
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                Console.WriteLine(jsonObj);
                return jsonObj["post"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return e + "#Error";
                //do something with caught exception
            }
        }
    }
}
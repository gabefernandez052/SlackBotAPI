using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SlackBotAPI.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SlackBotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SlackHandlerController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly string _slackBotEndPoint;
        private readonly int _randomSelector;
        private readonly string _yelpAPIKey;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="appSettings"></param>
        public SlackHandlerController(IOptions<AppSettings> appSettings)

        {
            _appSettings = appSettings.Value;

            _slackBotEndPoint = System.IO.File.ReadAllText(
                Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"Text\SlackWebHookEndPoint.txt"));

            _yelpAPIKey = System.IO.File.ReadAllText(
               Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"Text\Yelp.txt"));

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

                await client.PostAsync(_slackBotEndPoint,
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
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _yelpAPIKey);

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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SlackBotAPI.Models;

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
                var response = new { text = mention.Event.Text.Contains("Happy Hour", StringComparison.OrdinalIgnoreCase) ||
                                            mention.Event.Text.Contains("try again", StringComparison.OrdinalIgnoreCase)
                    ? $"How about {await GetHappyHourSuggestions()}?" 
                    : $"Sorry, <@{mention.Event.User}>. I don't know what you mean. Please ask me about happy hour" };

                await client.PostAsync(_slackBotEndPoint,
                        new StringContent(JsonConvert.SerializeObject(response), System.Text.Encoding.UTF8, "application/json"));

                #region Remove if unused
                //TODO - Possibly remove
                //if (!EqualityComparer<KeyValuePair<string, string>>.Default.Equals(user, default))
                //{
                //    var client = new HttpClient();
                //    var response = new { text = $"Don't you mean {user.Value}?" };
                //    await client.PostAsync(_slackBotEndPoint,
                //        new StringContent(JsonConvert.SerializeObject(response), System.Text.Encoding.UTF8, "application/json"));
                //}
                #endregion
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

                var task = client.GetAsync(endPoint).ContinueWith((taskResponse) =>
                    {
                        var jsonString = taskResponse.Result.Content.ReadAsStringAsync();
                        jsonString.Wait();
                        dtoResponse = JsonConvert.DeserializeObject<YelpDto>(jsonString.Result);
                    });

                task.Wait();
            }
            catch(Exception e)
            {
                //TODO - Add exception handling
            }
            finally
            {
                client?.Dispose();
            }
            
            return dtoResponse?.lstBusinesses[_randomSelector].Name;
        }
    }
}
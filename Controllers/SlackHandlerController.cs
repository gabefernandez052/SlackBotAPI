using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SlackBotAPI.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SlackBotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SlackHandlerController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly Dictionary<string, string> _secrets;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="appSettings"></param>
        public SlackHandlerController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _secrets = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(
               Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "secret.json")));
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
            var slack = new Slack(_appSettings);
            try
            {
                var response = new
                {
                    text = slack.ValidRequest(mention.Event.Text)
                    ? await slack.GetHappyHourSuggestions(_secrets["yelpAPIKey"])
                    : $"Sorry, <@{mention.Event.User}>. I don't know what you mean. Please ask me about happy hour"
                };

                await client.PostAsync(_secrets["slackBotEndPoint"],
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
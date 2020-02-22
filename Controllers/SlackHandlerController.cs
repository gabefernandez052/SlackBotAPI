using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;

namespace SlackBotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SlackHandlerController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly string _slackBotEndPoint;

        public SlackHandlerController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _slackBotEndPoint = System.IO.File.ReadAllText(
                Path.GetFullPath(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")) + "SlackWebHookEndPoint.txt");
        }

        [HttpPost]
        [Route("SlackBot")]
        public async void SlackBot(AppMention mention)
        {
            try
            {
                var user = _appSettings.SuggestedUserNames.FirstOrDefault(users => mention.Event.Text.Contains(users.Key));

                if (!EqualityComparer<KeyValuePair<string, string>>.Default.Equals(user, default))
                {
                    var client = new HttpClient();
                    var response = new { text = $"Don't you mean {user.Value}?" };

                    await client.PostAsync(_slackBotEndPoint,
                        new StringContent(JsonConvert.SerializeObject(response), System.Text.Encoding.UTF8, "application/json"));
                }
            }
            catch (Exception e)
            {
                //do something with caught exception
            }
        }
    }
}
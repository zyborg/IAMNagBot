using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Zyborg.IAMNagBot
{
    /// <summary>
    /// Simple client class to post chat messages to a Slack Channel or User.
    /// </summary>
    /// <remarks>
    /// See more details here:  https://api.slack.com/methods/chat.postMessage#channels
    /// </remarks>
    public class SlackChatPoster
    {
        public const string BearerAuthorizationScheme = "Bearer";
        public const string SlackApiBaseUrl = "https://slack.com";
        public const string ChatPostMessageApi = "/api/chat.postMessage";

        private string _oauthToken;
        private HttpClient _http;

        public SlackChatPoster(string oauthToken)
        {
            _oauthToken = oauthToken;
            _http = new HttpClient();


            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    BearerAuthorizationScheme, _oauthToken);
            _http.BaseAddress = new Uri(SlackApiBaseUrl);
        }

        public async Task<HttpResponseMessage> SendMessage(Dictionary<string, object> payload)
        {
            var rawJson = JsonConvert.SerializeObject(payload);
            var content = new StringContent(rawJson, Encoding.UTF8, "application/json");

            var resp = await _http.PostAsync(ChatPostMessageApi, content);
            //Console.WriteLine("Slack response: " + JsonConvert.SerializeObject(resp));
            return resp;
        }
    }
}
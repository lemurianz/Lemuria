using Lemuria.Server.Models;
using Lemuria.Server.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Lemuria.Server.Controllers
{
    public class FacebookController : ApiController
    {
        // Run this in command prompt to enable tunnel your localport
        // ngrok http -host-header="localhost:52232" 52232
        string pageToken = "";
        string appSecret = "";
        static SpeechServices speechService;

        private void InitializeShastaSpeech()
        {
            if (speechService == null)
            {
                speechService = new SpeechServices();
                if (!speechService.HasInitialized)
                    speechService.GetMessageReplies(); // Leave it async
            }
        }

        public HttpResponseMessage Get()
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            if (querystrings.Count > 0)
            {
                if (querystrings["hub.verify_token"] == "shasta")
                {
                    InitializeShastaSpeech();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                    };
                }
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }


        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            var signature = Request.Headers.GetValues("X-Hub-Signature").FirstOrDefault().Replace("sha1=", "");
            var body = await Request.Content.ReadAsStringAsync();
            if (!VerifySignature(signature, body))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            var value = JsonConvert.DeserializeObject<FacebookWebhookModels>(body);
            if (value._object != "page")
                return new HttpResponseMessage(HttpStatusCode.OK);

            foreach (var item in value.entry[0].messaging)
            {
                if (item.message == null && item.postback == null)
                    continue;
                else
                    await SendMessage(GetMessageTemplate(item.message.text, item.sender.id));
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private bool VerifySignature(string signature, string body)
        {
            var hashString = new StringBuilder();
            using (var crypto = new HMACSHA1(Encoding.UTF8.GetBytes(appSecret)))
            {
                var hash = crypto.ComputeHash(Encoding.UTF8.GetBytes(body));
                foreach (var item in hash)
                    hashString.Append(item.ToString("X2"));
            }

            return hashString.ToString().ToLower() == signature.ToLower();
        }


        /// <summary>
		/// get text message template
		/// </summary>
		/// <param name="text">text</param>
		/// <param name="sender">sender id</param>
		/// <returns>json</returns>
		private JObject GetMessageTemplate(string text, string sender)
        {
            return JObject.FromObject(new
            {
                recipient = new { id = sender },
                message = new { text = text }
            });
        }

        /// <summary>
        /// send message
        /// </summary>
        /// <param name="json">json</param>
        private async Task SendMessage(JObject json)
        {
            //Facebook ID of the user
            if ((string)json["recipient"]["id"] == "")
            {
                var message = (string)json["message"]["text"];
                // Incase it is not initialized
                InitializeShastaSpeech();
                if (speechService.HasInitialized)
                    message = speechService.ProcessMessage(message);
                else message = "Shasta speech service has not initialized yet";
                json["message"]["text"] = message;
            }
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage res = await client.PostAsync($"https://graph.facebook.com/v2.9/me/messages?access_token={pageToken}", new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Lemuria.Core.Server.Services;
using Lemuria.Core.Server.Models;
using System.IO;

namespace Lemuria.Core.Server.Controllers
{
    [Produces("application/json")]
    [Route("api/Facebook")]
    public class FacebookController : ApiController
    {
        // Run this in command prompt to enable tunnel your localport
        // ngrok http -host-header="localhost:37070" 37070
        string pageToken = "";
        string appSecret = "";
        static SpeechServices speechService;

        public FacebookController()
        {
            speechService = new SpeechServices();
        }

        public ActionResult Get()
        {
            var querystrings = ActionContext.HttpContext.Request.Query;
            if (querystrings.Count > 0)
            {
                if (querystrings["hub.verify_token"] == "shasta")
                {
                    return new ContentResult
                    {
                        Content = querystrings["hub.challenge"],
                        ContentType = "text/plain",
                        StatusCode = 200
                    };
                }
            }
            return new UnauthorizedResult();
        }


        [HttpPost]
        public async Task<ActionResult> Post()
        {
            var signature = ActionContext.HttpContext.Request.Headers.GetCommaSeparatedValues("X-Hub-Signature").FirstOrDefault().Replace("sha1=", "");
            var body = "";
            using (var requestBodyStream = new MemoryStream())
            {
                await ActionContext.HttpContext.Request.Body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                body = await new StreamReader(requestBodyStream).ReadToEndAsync();
            }
            if (!VerifySignature(signature, body))
                return new BadRequestResult();

            var value = JsonConvert.DeserializeObject<FacebookWebhookModels>(body);
            if (value._object != "page")
                return new OkResult();

            foreach (var item in value.entry[0].messaging)
            {
                if (item.message == null && item.postback == null)
                    continue;
                else
                    await SendMessage(GetMessageTemplate(item.message.text, item.sender.id));
            }

            return new OkResult();
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
                message = speechService.ProcessMessage(message);
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
using Lemuria.Server.Hubs;
using Lemuria.Server.Models;
using Lemuria.Server.Services;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Web.Http;

namespace Lemuria.Server.Controllers
{
    public class LemuriaController : ApiController
    {
        public IHubContext LemuriaContext { get; set; }
        public static SpeechServices speechService { get; set; }

        private void InitializeShastaSpeech()
        {
            if (speechService == null)
            {
                speechService = new SpeechServices();
                if (!speechService.HasInitialized)
                    speechService.GetMessageReplies(); // Leave it async
            }
        }

        public LemuriaController()
        {
            LemuriaContext = GlobalHost.ConnectionManager.GetHubContext<LemuriaHub>();
            InitializeShastaSpeech();
        }

        public string Get()
        {
            if (LemuriaHub.SetConnected)
                return "Lemurian module is online";
            else return "Lemurian module is offline";
        }

        public string Get(string message)
        {
            if (speechService.HasInitialized)
                message = speechService.ProcessMessage(message);
            else message = "Shasta speech service has not initialized yet";
            return message;
        }

    }
}

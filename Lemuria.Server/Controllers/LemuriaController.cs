using Lemuria.Server.Hubs;
using Lemuria.Server.Models;
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

        public LemuriaController()
        {
            LemuriaContext = GlobalHost.ConnectionManager.GetHubContext<LemuriaHub>();
        }

        public string Get()
        {
            if (LemuriaHub.SetConnected)
                return "Lemurian module is online";
            else return "Lemurian module is offline";
        }

    }
}

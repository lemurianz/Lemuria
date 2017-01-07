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
                return "Lemuria is online";
            else return "Lemuria is offline";
        }

        public void Get(bool isConnect)
        {
            LemuriaHub.SetConnected = isConnect;
            LemuriaContext.Clients.All.OnConnectionChanged(isConnect);
        }

        public LemuriaModels Get(bool settings, string password)
        {
            if (password == "lemuria" & LemuriaHub.SetConnected)
            {
                if (settings == true)
                    return new LemuriaModels
                    {
                        SetConnected = LemuriaHub.SetConnected,
                        FBTag = LemuriaHub.FBTag,
                        LRTag = LemuriaHub.LRTag,
                        DirectionSpeed = LemuriaHub.DirectionSpeed,
                        TurnSpeed = LemuriaHub.TurnSpeed
                    };
                else return null;
            }
            else return null;
        }

        public void Get(string fbTag, string lrTag, int directionSpeed, int turnSpeed)
        {
            LemuriaHub.FBTag = fbTag;
            LemuriaHub.LRTag = lrTag;
            LemuriaHub.DirectionSpeed = directionSpeed;
            LemuriaHub.TurnSpeed = turnSpeed;
            LemuriaContext.Clients.All.NavigateRobot(new Tuple<string, string, int, int>(fbTag, lrTag, directionSpeed, turnSpeed));
        }
    }
}

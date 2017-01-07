using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lemuria.Server.Hubs
{
    public class LemuriaHub : Hub
    {
        public static bool SetConnected { get; set; }
        public static string FBTag { get; set; }
        public static string LRTag { get; set; }
        public static int DirectionSpeed { get; set; }
        public static int TurnSpeed { get; set; }
    }
}
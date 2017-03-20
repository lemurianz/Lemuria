using Lemuria.Server.Models;
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
        public static Dictionary<string, string> SignalRClients { get; set; }
        public static string FBTag { get; set; }
        public static string LRTag { get; set; }
        public static int DirectionSpeed { get; set; }
        public static int TurnSpeed { get; set; }


        public LemuriaHub()
        {
            if (SignalRClients == null) {
                SignalRClients = new Dictionary<string, string>();
                SignalRClients.Add("motor", "");
                SignalRClients.Add("master", "");
            }
        }

        public bool LemurianSignal(string type)
        {
            // Master Controller
            if (type == "master")
                SignalRClients["master"] = this.Context.ConnectionId;
            // Motor
            else if (type == "motor")
            {
                SignalRClients["motor"] = this.Context.ConnectionId;
                SetConnected = true;
            }
            // Deal with others
            else SignalRClients.Add(this.Context.ConnectionId, type);
            return true;
        }

        public void LemurianMove(string direction, MotorSpeedModels speed)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (masterId == this.Context.ConnectionId & !string.IsNullOrEmpty(motorId))
                this.Clients.Client(motorId).MoveRobot(direction, speed.ForwardSpeed, speed.BackwardSpeed, speed.LeftSpeed, speed.RightSpeed);
        }
    }
}
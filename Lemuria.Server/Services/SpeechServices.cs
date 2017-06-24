using Lemuria.Server.Hubs;
using Lemuria.Server.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web;

namespace Lemuria.Server.Services
{
    public class SpeechServices
    {
        public List<MessageReplyModels> MessageReplies { get; set; }
        public bool HasInitialized = false;
        public string _INT = "{int}", _STR = "{str}";
        public LemuriaHub LemurianHub { get; set; }
        public static bool MovementStatus { get; set; }
        public static bool MoveLoopBreak { get; set; }
        public static Timer MovementTimer { get; set; }

        public SpeechServices()
        {
            DefaultHubManager LemurianResolver = new DefaultHubManager(GlobalHost.DependencyResolver);
            LemurianHub = LemurianResolver.ResolveHub("LemuriaHub") as LemuriaHub;
            LemurianHub.LemurianSignal("messenger");
            if (MovementTimer == null)
            {
                MovementTimer = new Timer();
                MovementTimer.Elapsed += MovementTimer_Tick;
                MovementTimer.AutoReset = false;
            }
        }

        public async Task<bool> GetMessageReplies()
        {
            using (StreamReader sr = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/Data/MessageReplies.json")))
                MessageReplies = JsonConvert.DeserializeObject<List<MessageReplyModels>>(await sr.ReadToEndAsync());

            HasInitialized = true;

            return HasInitialized;
        }

        // Move fuctions
        private void StopMovement()
        {
            MotorSpeedModels speed = new MotorSpeedModels();
            speed.ForwardSpeed = 0;
            speed.BackwardSpeed = 0;
            speed.LeftSpeed = 0;
            speed.RightSpeed = 0;
            MovementTimer.Stop();
            LemurianHub.LemurianMove("stop", speed);
        }

        private void MovementTimer_Tick(object sender, object e)
        {
            StopMovement();
        }

        private void MoveFunctions(string timer, string direction, MotorSpeedModels speed)
        {
            MovementTimer.Stop();

            if (direction == "Stop")
                MovementStatus = false;
            else MovementStatus = true;

            if (MovementStatus & direction != "Stop")
            {
                MovementTimer.Interval = Convert.ToInt32(timer) * 1000;
                LemurianHub.LemurianMove(direction.ToLower(), speed);
                MovementTimer.Start();
            }
            else
                StopMovement();
        }

        private string MoveCommands(string returnMessage, MessageReplyModels messageReply, Match m, string key)
        {
            MotorSpeedModels speed = new MotorSpeedModels();
            var direction = messageReply.Methods[key][0];
            speed.ForwardSpeed = Convert.ToInt32(messageReply.Methods[key][1]);
            speed.BackwardSpeed = Convert.ToInt32(messageReply.Methods[key][2]);
            speed.LeftSpeed = Convert.ToInt32(messageReply.Methods[key][3]);
            speed.RightSpeed = Convert.ToInt32(messageReply.Methods[key][4]);
            var timer = messageReply.Methods[key][5];
            if (timer == _INT)
            {
                timer = m.Groups[2].Value;
                returnMessage = returnMessage.Replace(_INT, timer);
            }

            MoveFunctions(timer, direction, speed);

            return returnMessage;
        }

        // Music functions
        private void MusicFuctions(string status, bool isSearch)
        {
            if (status == "Stop")
                LemurianHub.LemurianMusic(false, "", isSearch);
            else LemurianHub.LemurianMusic(true, status, isSearch);
        }

        private string MusicCommands(string returnMessage, MessageReplyModels messageReply, Match m, string key)
        {
            var isSearch = false;
            var playMusic = messageReply.Methods[key][0];
            if (playMusic == _STR)
            {
                playMusic = m.Groups[2].Value;
                isSearch = true;
            }
            else if(playMusic == "Top Trending Music")
                isSearch = true;

            var danceStatus = messageReply.Methods[key][1];
            if (danceStatus == _STR & m.Value.Contains("dance"))
            {
                danceStatus = "dance";
                returnMessage = returnMessage.Replace(_STR, " and here I am dancing");
            }
            else
            {
                danceStatus = "";
                returnMessage = returnMessage.Replace(_STR, "");
            }

            MusicFuctions(playMusic, isSearch);

            return returnMessage;
        }

        // Volume functions
        private string VolumeCommands(string returnMessage, MessageReplyModels messageReply, Match m, string key)
        {
            var volume = messageReply.Methods[key][0];
            if (volume == _INT)
            {
                volume = m.Groups[2].Value;
                returnMessage = returnMessage.Replace(_INT, volume);
            }

            LemurianHub.LemurianVolume(Convert.ToInt32(volume));

            return returnMessage;
        }

        // Greeting functions
        private string GreetingCommands(string returnMessage, MessageReplyModels messageReply, string key)
        {
            var greeting = messageReply.Methods[key][0];
            LemurianHub.LemurianGreeting(greeting);
            return returnMessage;
        }

        public string ProcessMessage(string message)
        {
            foreach (var messageReply in MessageReplies)
            {
                Regex r = new Regex(messageReply.ReceivedMessage, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Match m = r.Match(message);
                if (m.Success)
                {
                    var returnMessage = messageReply.ReplyMessage;
                    var key = messageReply.Methods.Keys.First();

                    // Movements
                    if (key == "Move")
                        returnMessage = MoveCommands(returnMessage, messageReply, m, key);

                    // Music
                    else if (key == "Music")
                        returnMessage = MusicCommands(returnMessage, messageReply, m, key);

                    // Volume
                    else if (key == "Volume")
                        returnMessage = VolumeCommands(returnMessage, messageReply, m, key);

                    // Greeting
                    else if (key == "Greetings")
                        returnMessage = GreetingCommands(returnMessage, messageReply, key);

                    return returnMessage;
                }
            }

            return "Sorry I got much to learn and this is one";
        }
    }
}
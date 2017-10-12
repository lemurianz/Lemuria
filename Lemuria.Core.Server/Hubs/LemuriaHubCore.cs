using Lemuria.Core.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lemuria.Core.Server.Hubs
{
    public class LemuriaHubCore : Hub
    {
        public static bool SetConnected { get; set; }
        public static Dictionary<string, string> SignalRClients { get; set; }
        public static SettingsModels Settings { get; set; }
        public LemuriaHubCore LemurianHubContext { get; set; }

        public LemuriaHubCore()
        {
            if (SignalRClients == null)
            {
                SignalRClients = new Dictionary<string, string>();
                SignalRClients.Add("motor", "");
                SignalRClients.Add("master", "");
            }
            if (Settings == null) Settings = new SettingsModels();
            LemurianHubContext = this;
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
            // Facebook messenger as controller
            else if (type == "messenger")
            {
                if (string.IsNullOrEmpty(SignalRClients["master"]))
                    SignalRClients["master"] = "messenger";
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
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).InvokeAsync("MoveRobot", new object[] { direction, speed.ForwardSpeed, speed.BackwardSpeed, speed.LeftSpeed, speed.RightSpeed });
        }

        public void LemurianMusic(bool play, string youtubeId, bool isSearch)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).InvokeAsync("YoutubeMusic", new object[] { play, youtubeId, isSearch });
        }

        public void LemurianVolume(int volume)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).InvokeAsync("SetVolume", new object[] { volume });
        }

        public void LemurianLoadText(string type, string text)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).InvokeAsync("SetLoadText", new object[] { type, text });
        }

        public void LemurianGreeting(string greeting)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).InvokeAsync("SetGreeting", new object[] { greeting });
        }

        public SettingsModels GetLemurianSettings(string type)
        {
            SettingsModels settings = new SettingsModels();
            switch (type)
            {
                case "motor":
                case "master":
                    if (settings.MaxMotorSpeedA == 0) Settings.MaxMotorSpeedA = 100;
                    if (settings.MaxMotorSpeedB == 0) Settings.MaxMotorSpeedB = 100;
                    settings = Settings;
                    break;
                default:
                    break;
            }
            return settings;
        }

        private bool ControllerMotorPass()
        {
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (!string.IsNullOrEmpty(masterId) & !string.IsNullOrEmpty(motorId))
            {
                return true;
            }
            else return false;
        }

        private bool MotorControllerPass()
        {
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (motorId == this.Context.ConnectionId & !string.IsNullOrEmpty(masterId))
                return true;
            else return false;
        }

        // Set the settings
        //Motor
        public bool SetMaxMotorASpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.MaxMotorSpeedA = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetMotorAMaxSpeed", new object[] { Settings.MaxMotorSpeedA });
            }
            return true;
        }

        public bool SetMaxMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.MaxMotorSpeedB = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetMotorBMaxSpeed", new object[] { Settings.MaxMotorSpeedB });
            }
            return true;
        }

        public bool SetStartMotorASpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedA = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetStartMotorASpeed", new object[] { Settings.StartMotorSpeedA });
            }
            return true;
        }

        public bool SetStartMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedB = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetStartMotorBSpeed", new object[] { Settings.StartMotorSpeedB });
            }
            return true;
        }

        // IR
        public bool SetTopLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopLeftIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetTopLeftIRSensor", new object[] { Settings.TopLeftIR });
            }
            return true;
        }

        public bool SetTopRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopRightIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetTopRightIRSensor", new object[] { Settings.TopRightIR });
            }
            return true;
        }

        public bool SetBottomLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomLeftIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetBottomLeftIRSensor", new object[] { Settings.BottomLeftIR });
            }
            return true;
        }

        public bool SetBottomRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomRightIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetBottomRightIRSensor", new object[] { Settings.BottomRightIR });
            }
            return true;
        }

        // Sonar
        public bool SetFrontSonarSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.FrontSonar = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetFrontSonarSensor", new object[] { Settings.FrontSonar });
            }
            return true;
        }

        public bool SetBackSonarSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BackSonar = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetBackSonarSensor", new object[] { Settings.BackSonar });
            }
            return true;
        }

        // Temperature
        public bool SetTemperatureHumiditySensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.InternalTempAndHum = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetTemperatureHumiditySensor", new object[] { Settings.InternalTempAndHum });
            }
            return true;
        }

        // Face detection
        public bool SetFaceDetectionCamera(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.FaceDetect = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).InvokeAsync("SetFaceDetectionCamera", new object[] { Settings.FaceDetect });
            }
            return true;
        }

        // Set Notifications to Controller
        public void NotifyTopLeftIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifyTopLeftIR", new object[] { value });
        }

        public void NotifyTopRightIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifyTopRightIR", new object[] { value });
        }

        public void NotifyBottomLeftIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifyBottomLeftIR", new object[] { value });
        }

        public void NotifyBottomRightIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifyBottomRightIR", new object[] { value });
        }

        public void NotifySonarDistance(double distance)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifySonarDistance", new object[] { distance });
        }

        public void NotifyTemperatureHumidity(double temperature, double humidity)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).InvokeAsync("NotifyTemperatureHumidity", new object[] { temperature, humidity });
        }

    }
}

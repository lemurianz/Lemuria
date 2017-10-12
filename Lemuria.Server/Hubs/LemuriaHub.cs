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
        public static SettingsModels Settings { get; set; }
        public IHubContext LemurianHubContext { get; set; }

        public LemuriaHub()
        {
            if (SignalRClients == null) {
                SignalRClients = new Dictionary<string, string>();
                SignalRClients.Add("motor", "");
                SignalRClients.Add("master", "");
            }
            if (Settings == null) Settings = new SettingsModels();
            LemurianHubContext = GlobalHost.ConnectionManager.GetHubContext<LemuriaHub>();
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
                LemurianHubContext.Clients.Client(motorId).MoveRobot(direction, speed.ForwardSpeed, speed.BackwardSpeed, speed.LeftSpeed, speed.RightSpeed);
        }

        public void LemurianMusic(bool play, string youtubeId, bool isSearch)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).YoutubeMusic(play, youtubeId, isSearch);
        }

        public void LemurianVolume(int volume)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).SetVolume(volume);
        }

        public void LemurianLoadText(string type, string text)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).SetLoadText(type, text);
        }

        public void LemurianGreeting(string greeting)
        {
            // only get movement controls from the master controller and 
            // send the navigation info only to the motor
            var masterId = SignalRClients["master"];
            var motorId = SignalRClients["motor"];
            if (ControllerMotorPass())
                LemurianHubContext.Clients.Client(motorId).SetGreeting(greeting);
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
            if (ControllerMotorPass()) { 
                Settings.MaxMotorSpeedA = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetMotorAMaxSpeed(Settings.MaxMotorSpeedA);
            }
            return true;
        }

        public bool SetMaxMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.MaxMotorSpeedB = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetMotorBMaxSpeed(Settings.MaxMotorSpeedB);
            }
            return true;
        }

        public bool SetStartMotorASpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedA = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetStartMotorASpeed(Settings.StartMotorSpeedA);
            }
            return true;
        }

        public bool SetStartMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedB = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetStartMotorBSpeed(Settings.StartMotorSpeedB);
            }
            return true;
        }

        // IR
        public bool SetTopLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopLeftIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetTopLeftIRSensor(Settings.TopLeftIR);
            }
            return true;
        }

        public bool SetTopRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopRightIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetTopRightIRSensor(Settings.TopRightIR);
            }
            return true;
        }

        public bool SetBottomLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomLeftIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetBottomLeftIRSensor(Settings.BottomLeftIR);
            }
            return true;
        }

        public bool SetBottomRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomRightIR = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetBottomRightIRSensor(Settings.BottomRightIR);
            }
            return true;
        }

        // Sonar
        public bool SetFrontSonarSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.FrontSonar = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetFrontSonarSensor(Settings.FrontSonar);
            }
            return true;
        }

        public bool SetBackSonarSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BackSonar = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetBackSonarSensor(Settings.BackSonar);
            }
            return true;
        }

        // Temperature
        public bool SetTemperatureHumiditySensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.InternalTempAndHum = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetTemperatureHumiditySensor(Settings.InternalTempAndHum);
            }
            return true;
        }

        // Face detection
        public bool SetFaceDetectionCamera(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.FaceDetect = value;
                LemurianHubContext.Clients.Client(SignalRClients["motor"]).SetFaceDetectionCamera(Settings.FaceDetect);
            }
            return true;
        }

        // Set Notifications to Controller
        public void NotifyTopLeftIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifyTopLeftIR(value);
        }

        public void NotifyTopRightIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifyTopRightIR(value);
        }

        public void NotifyBottomLeftIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifyBottomLeftIR(value);
        }

        public void NotifyBottomRightIR(bool value)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifyBottomRightIR(value);
        }

        public void NotifySonarDistance(double distance)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifySonarDistance(distance);
        }

        public void NotifyTemperatureHumidity(double temperature, double humidity)
        {
            if (MotorControllerPass())
                LemurianHubContext.Clients.Client(SignalRClients["master"]).NotifyTemperatureHumidity(temperature, humidity);
        }

    }
}
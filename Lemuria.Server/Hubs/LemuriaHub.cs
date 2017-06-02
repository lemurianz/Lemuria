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


        public LemuriaHub()
        {
            if (SignalRClients == null) {
                SignalRClients = new Dictionary<string, string>();
                SignalRClients.Add("motor", "");
                SignalRClients.Add("master", "");
            }
            if (Settings == null) Settings = new SettingsModels();
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
            if (masterId == this.Context.ConnectionId & !string.IsNullOrEmpty(motorId))
                return true;
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
        public bool SetMaxMotorASpeed(int value)
        {
            if (ControllerMotorPass()) { 
                Settings.MaxMotorSpeedA = value;
                this.Clients.Client(SignalRClients["motor"]).SetMotorAMaxSpeed(Settings.MaxMotorSpeedA);
            }
            return true;
        }

        public bool SetMaxMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.MaxMotorSpeedB = value;
                this.Clients.Client(SignalRClients["motor"]).SetMotorBMaxSpeed(Settings.MaxMotorSpeedB);
            }
            return true;
        }

        public bool SetStartMotorASpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedA = value;
                this.Clients.Client(SignalRClients["motor"]).SetStartMotorASpeed(Settings.StartMotorSpeedA);
            }
            return true;
        }

        public bool SetStartMotorBSpeed(int value)
        {
            if (ControllerMotorPass())
            {
                Settings.StartMotorSpeedB = value;
                this.Clients.Client(SignalRClients["motor"]).SetStartMotorBSpeed(Settings.StartMotorSpeedB);
            }
            return true;
        }

        public bool SetTopLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopLeftIR = value;
                this.Clients.Client(SignalRClients["motor"]).SetTopLeftIRSensor(Settings.TopLeftIR);
            }
            return true;
        }

        public bool SetTopRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.TopRightIR = value;
                this.Clients.Client(SignalRClients["motor"]).SetTopRightIRSensor(Settings.TopRightIR);
            }
            return true;
        }

        public bool SetBottomLeftIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomLeftIR = value;
                this.Clients.Client(SignalRClients["motor"]).SetBottomLeftIRSensor(Settings.BottomLeftIR);
            }
            return true;
        }

        public bool SetBottomRightIRSensor(bool value)
        {
            if (ControllerMotorPass())
            {
                Settings.BottomRightIR = value;
                this.Clients.Client(SignalRClients["motor"]).SetBottomRightIRSensor(Settings.BottomRightIR);
            }
            return true;
        }

        public bool SetFrontSonarSensor(bool value)
        {
            Settings.FrontSonar = value;
            return true;
        }

        public bool SetBackSonarSensor(bool value)
        {
            Settings.BackSonar = value;
            return true;
        }

        public bool SetTemperatureHumiditySensor(bool value)
        {
            Settings.InternalTempAndHum = value;
            return true;
        }

        public bool SetFaceDetectionCamera(bool value)
        {
            Settings.FaceDetect = value;
            return true;
        }

        // Set Notifications to Controller
        public void NotifyTopLeftIR(bool value)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifyTopLeftIR(value);
        }

        public void NotifyTopRightIR(bool value)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifyTopRightIR(value);
        }

        public void NotifyBottomLeftIR(bool value)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifyBottomLeftIR(value);
        }

        public void NotifyBottomRightIR(bool value)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifyBottomRightIR(value);
        }

        public void NotifySonarDistance(double distance)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifySonarDistance(distance);
        }

        public void NotifyTemperatureHumidity(double temperature, double humidity)
        {
            if (MotorControllerPass())
                this.Clients.Client(SignalRClients["master"]).NotifyTemperatureHumidity(temperature, humidity);
        }

    }
}
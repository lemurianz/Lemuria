using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.UI.Xaml.Media;

namespace Shasta.Helpers
{
    class LemuriaSettings
    {
        // Motor Driver
        public int MaxMotorASpeed { get; set; }
        public int MaxMotorBSpeed { get; set; }
        // Floor Detection
        public bool InfraSensor1 { get; set; }
        public bool InfraSensor2 { get; set; }
        public bool InfraSensor3 { get; set; }
        public bool InfraSensor4 { get; set; }
        // Object Avoidance
        public bool SonarSensor { get; set; }
        // Temperature
        public bool TemperatureSensor { get; set; }
    }

    class StaticComponents
    {
        // Motor pins
        public const int ENABLE_L293D_MOTOR_SPEED_A = 13;
        public const int MOTOR_DIRECTION_FORWARD_A = 5;
        public const int MOTOR_DIRECTION_BACKWARD_A = 6;
        public const int ENABLE_L293D_MOTOR_SPEED_B = 12;
        public const int MOTOR_DIRECTION_FORWARD_B = 22;
        public const int MOTOR_DIRECTION_BACKWARD_B = 27;
        public const int SONAR_TRIGGER = 23;
        public const int SONAR_ECHO = 24;
        public const int INFRA_1 = 4;
        public const int INFRA_2 = 17;
        public const int INFRA_3 = 26;
        public const int INFRA_4 = 25;
        public const int TEMPERATURE_HUMIDITY = 16;

        // PWM
        public static PwmPin speedAPin { get; set; }
        public static PwmPin speedBPin { get; set; }

        // GPIO
        public static GpioPin directionForwardAPin { get; set; }
        public static GpioPin directionBackwardAPin { get; set; }
        public static GpioPin directionForwardBPin { get; set; }
        public static GpioPin directionBackwardBPin { get; set; }
        public static GpioPin triggerPin { get; set; }
        public static GpioPin echoPin { get; set; }
        public static GpioPin infra1Pin { get; set; }
        public static GpioPin infra2Pin { get; set; }
        public static GpioPin infra3Pin { get; set; }
        public static GpioPin infra4Pin { get; set; }


        // Indicating colors
        public static SolidColorBrush activeBrush = new SolidColorBrush(Windows.UI.Colors.OrangeRed);
        public static SolidColorBrush inactiveBrush = new SolidColorBrush(Windows.UI.Colors.Black);

        // Lemuria Hub
        public static bool IsLemuriaHubConnected { get; set; }
        public static string LocalWifiHub = "http://192.168.0.57:52232/";
        public static bool IsAuthorised { get; set; }
        public static string UserName { get; set; }
        public static LemuriaSettings LemuriaSettings { get; set; }

        // Timer
        public static  Stopwatch StopWatch = new Stopwatch();

        // Oxford Face API Primary should be entered here
        // You can obtain a subscription key for Face API by following the instructions here: https://www.microsoft.com/cognitive-services/en-us/sign-up
        public const string OxfordAPIKey = "3532ca2ce6d34340b58f6246452d2355";

        // Name of the folder in which all Whitelist data is stored
        public const string WhiteListFolderName = "Lemuria Whitelist";
    }
}

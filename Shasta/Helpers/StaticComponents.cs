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
    class StaticComponents
    {
        // Motor pins
        public const int ENABLE_L293D_MOTOR_SPEED = 13;
        public const int MOTOR_DIRECTION_FORWARD = 5;
        public const int MOTOR_DIRECTION_BACKWARD = 6;
        public const int ENABLE_L293D_MOTOR_TURN = 12;
        public const int MOTOR_TURN_LEFT = 22;
        public const int MOTOR_TURN_RIGHT = 27;
        public const int SONAR_TRIGGER = 23;
        public const int SONAR_ECHO = 24;
        public const int INFRA_1 = 4;
        public const int INFRA_2 = 17;
        public const int INFRA_3 = 26;
        public const int INFRA_4 = 25;
        public const int TEMPERATURE_HUMIDITY = 16;

        // PWM
        public static PwmPin speedPin { get; set; }
        public static PwmPin turnPin { get; set; }

        // GPIO
        public static GpioPin directionForwardPin { get; set; }
        public static GpioPin directionBackwardPin { get; set; }
        public static GpioPin turnLeftPin { get; set; }
        public static GpioPin turnRightPin { get; set; }
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
        public static string LocalWifiHub = "http://192.168.0.53:52232/";

        // Timer
        public static  Stopwatch StopWatch = new Stopwatch();

        // Oxford Face API Primary should be entered here
        // You can obtain a subscription key for Face API by following the instructions here: https://www.microsoft.com/cognitive-services/en-us/sign-up
        public const string OxfordAPIKey = "3532ca2ce6d34340b58f6246452d2355";

        // Name of the folder in which all Whitelist data is stored
        public const string WhiteListFolderName = "Lemuria Whitelist";
    }
}

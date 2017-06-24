using Shasta.Models;
using Shasta.ViewModels;
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
        public const int ENABLE_L293D_MOTOR_SPEED_A = 12;
        public const int MOTOR_DIRECTION_FORWARD_A = 22;
        public const int MOTOR_DIRECTION_BACKWARD_A = 27;
        public const int ENABLE_L293D_MOTOR_SPEED_B = 13;
        public const int MOTOR_DIRECTION_FORWARD_B = 5;
        public const int MOTOR_DIRECTION_BACKWARD_B = 6;
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
        public static string LocalWifiHub = "http://192.168.0.95:52232/";
        public static bool IsAuthorised { get; set; }
        public static string UserName { get; set; }
        public static SettingsModels LemuriaSettings { get; set; }

        // Timer
        public static  Stopwatch StopWatch = new Stopwatch();
        public const int FloorDetectionSensorDelay = 1000;
        public const int ObjectAvoidanceSensorDelay = 500;
        public const int TempHumSensorDelay = 2000;

        // Oxford Face API Primary should be entered here
        // You can obtain a subscription key for Face API by following the instructions here: https://www.microsoft.com/cognitive-services/en-us/sign-up
        public const string OxfordAPIKey = "";

        // Name of the folder in which all Whitelist data is stored
        public const string WhiteListFolderName = "Lemuria Whitelist";

        // Openweather API Key
        public const string OpenWeatherURL = "http://api.openweathermap.org/data/2.5/weather?units=metric";
        public const string OpenWeatherAPIKey = "&appid=";

        // Youtube API Key
        public const string YoutubeMostPopularURL = "https://www.googleapis.com/youtube/v3/videos?part=snippet&chart=mostPopular";
        public const string YoutubeSearchURL = "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=15";
        public const string YoutubeAPIKey = "&key=";
    }
}

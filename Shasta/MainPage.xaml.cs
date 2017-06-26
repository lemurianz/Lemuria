using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.IoT.Lightning.Providers;
using Microsoft.ProjectOxford.Face;
using Newtonsoft.Json;
using Sensors.Dht;
using Shasta.Facial_Recognition;
using Shasta.Helpers;
using Shasta.Models;
using Shasta.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VideoLibrary;
using Windows.ApplicationModel.Core;
using Windows.Devices;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Devices;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Shasta
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        #region Global Variables

        private WebcamHelper webcam;
        private StorageFile currentIdPhotoFile;

        // Whitelist Related Variables:
        private StorageFolder whitelistFolder;
        private bool currentlyUpdatingWhitelist;

        // Oxford Related Variables:
        private bool initializedOxford = false;

        // create instance of a DHT11 
        private IDht _dhtInterface = null;

        // The media object for controlling and playing audio.
        private SpeechSynthesizer speechSynthesizer = null;
        private List<string> textReadback = new List<string>();

        // Infra update
        private InfraSonarUpdateModels infraUpdate = null;
        public IHubProxy LemurianHub = null;

        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(800, 480);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            speechSynthesizer = new SpeechSynthesizer();
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
            //ReadText("Hi, I am " + speechSynthesizer.Voice.DisplayName);
            //StaticComponents.IsLemuriaHubConnected = true;
            LemuriaConnect();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitGPIO();
            InitHelpers();
            InitOxford();
            UpdateSpeakerStatus();

            //  This if you are testing the app on Windows machices
            if (StaticComponents.IsOnWindows)
            {
                LemuriaHubStat.Text = "Loading";
                InitHub();
            }
        }

        #region Initialize

        private async void InitGPIO()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

                var gpio = await GpioController.GetDefaultAsync();
                string status = "";
                // Show an error if there is no GPIO controller
                if (gpio == null)
                {
                    GpioStatus.Text = "There is no GPIO controller on this device.";
                    return;
                }

                status = "GPIO is using Lightning provider and ";
                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // Use the pwmController on raspberry pi
                pwmController.SetDesiredFrequency(50); // set frequency to 50Hz
                var gpioControllers = await GpioController.GetControllersAsync(LightningGpioProvider.GetGpioProvider());
                var gpioController = gpioControllers[0];

                // Enable the L293D drivers Motor Speed A
                StaticComponents.speedAPin = pwmController.OpenPin(StaticComponents.ENABLE_L293D_MOTOR_SPEED_A);
                StaticComponents.speedAPin.SetActiveDutyCyclePercentage(0);
                StaticComponents.speedAPin.Start();

                // Enable direction forward
                StaticComponents.directionForwardAPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_FORWARD_A);
                StaticComponents.directionForwardAPin.Write(GpioPinValue.Low);
                StaticComponents.directionForwardAPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable direction backward
                StaticComponents.directionBackwardAPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_BACKWARD_A);
                StaticComponents.directionBackwardAPin.Write(GpioPinValue.Low);
                StaticComponents.directionBackwardAPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable the L293D drivers Motor Turn
                StaticComponents.speedBPin = pwmController.OpenPin(StaticComponents.ENABLE_L293D_MOTOR_SPEED_B);
                StaticComponents.speedBPin.SetActiveDutyCyclePercentage(0);
                StaticComponents.speedBPin.Start();

                // Enable turn left
                StaticComponents.directionForwardBPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_FORWARD_B);
                StaticComponents.directionForwardBPin.Write(GpioPinValue.Low);
                StaticComponents.directionForwardBPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable turn right
                StaticComponents.directionBackwardBPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_BACKWARD_B);
                StaticComponents.directionBackwardBPin.Write(GpioPinValue.Low);
                StaticComponents.directionBackwardBPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable sonar echo
                StaticComponents.echoPin = gpioController.OpenPin(StaticComponents.SONAR_ECHO);
                StaticComponents.echoPin.SetDriveMode(GpioPinDriveMode.Input);

                // Enable sonar trigger
                StaticComponents.triggerPin = gpioController.OpenPin(StaticComponents.SONAR_TRIGGER);
                StaticComponents.triggerPin.Write(GpioPinValue.Low);
                StaticComponents.triggerPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable infra 1
                StaticComponents.infra1Pin = gpioController.OpenPin(StaticComponents.INFRA_1);
                StaticComponents.infra1Pin.SetDriveMode(GpioPinDriveMode.Input);

                // Enable infra 2
                StaticComponents.infra2Pin = gpioController.OpenPin(StaticComponents.INFRA_2);
                StaticComponents.infra2Pin.SetDriveMode(GpioPinDriveMode.Input);

                // Enable infra 3
                StaticComponents.infra3Pin = gpioController.OpenPin(StaticComponents.INFRA_3);
                StaticComponents.infra3Pin.SetDriveMode(GpioPinDriveMode.Input);

                // Enable infra 4
                StaticComponents.infra4Pin = gpioController.OpenPin(StaticComponents.INFRA_4);
                StaticComponents.infra4Pin.SetDriveMode(GpioPinDriveMode.Input);

                // Enable Temperature Humidity
                // Create instance of a DHT11 
                GpioPin temperatureHumidityPin = gpioController.OpenPin(StaticComponents.TEMPERATURE_HUMIDITY, GpioSharingMode.Exclusive);
                _dhtInterface = new Dht11(temperatureHumidityPin, GpioPinDriveMode.Input);

                GpioStatus.Text = status + "GPIO pins initialized correctly.";

                ReadText(GpioStatus.Text, true);

                // Initialize the Lemuria Hub
                LemuriaHubStat.Text = "Loading";
                InitHub();
            }
            else
            {
                GpioStatus.Text = "LightningProvider is not enabled";
            }

        }

        private void InitSettingsAndEvents()
        {
            infraUpdate = new InfraSonarUpdateModels();
            infraUpdate.FrontSonarDistance = 500;
            FrontSonar.IsOn = StaticComponents.LemuriaSettings.FrontSonar;
            Infra1Detect.IsOn = StaticComponents.LemuriaSettings.TopLeftIR;
            Infra2Detect.IsOn = StaticComponents.LemuriaSettings.TopRightIR;
            Infra3Detect.IsOn = StaticComponents.LemuriaSettings.BottomLeftIR;
            Infra4Detect.IsOn = StaticComponents.LemuriaSettings.BottomRightIR;
            GetTemperatureHumidity.IsOn = StaticComponents.LemuriaSettings.InternalTempAndHum;
        }

        private async void InitHub()
        {
            try
            {
                // Start the Lemuria
                // Enable UI Controllers for shasta without the server
                var status = "";
                if (StaticComponents.IsWithoutServer)
                {
                    StaticComponents.IsLemuriaHubConnected = true;
                    StaticComponents.LemuriaSettings = new SettingsModels();
                    status = "Connected (Test Mode)";
                }
                else
                {
                    var hubConnection = new HubConnection(StaticComponents.LocalWifiHub);
                    LemurianHub = hubConnection.CreateHubProxy("LemuriaHub");
                    LemurianHub.On<string, int, int, int, int>("MoveRobot", MoveRobotCallback);
                    LemurianHub.On<int>("SetStartMotorASpeed", SetStartMotorASpeedCallback);
                    LemurianHub.On<int>("SetStartMotorBSpeed", SetStartMotorBSpeedCallback);
                    LemurianHub.On<int>("SetMotorAMaxSpeed", SetMotorAMaxSpeedCallback);
                    LemurianHub.On<int>("SetMotorBMaxSpeed", SetMotorBMaxSpeedCallback);
                    LemurianHub.On<bool>("SetTopLeftIRSensor", SetTopLeftIRSensorCallback);
                    LemurianHub.On<bool>("SetTopRightIRSensor", SetTopRightIRSensorCallback);
                    LemurianHub.On<bool>("SetBottomLeftIRSensor", SetBottomLeftIRSensorCallback);
                    LemurianHub.On<bool>("SetBottomRightIRSensor", SetBottomRightIRSensorCallback);
                    LemurianHub.On<bool>("SetFrontSonarSensor", SetFrontSonarSensorCallback);
                    LemurianHub.On<bool>("SetBackSonarSensor", SetBackSonarSensorCallback);
                    LemurianHub.On<bool>("SetTemperatureHumiditySensor", SetTemperatureHumiditySensorCallback);
                    LemurianHub.On<bool>("SetFaceDetectionCamera", SetFaceDetectionCameraCallback);
                    LemurianHub.On<bool, string, bool>("YoutubeMusic", YoutubeMusicCallback);
                    LemurianHub.On<int>("SetVolume", SetVolumeCallback);
                    LemurianHub.On<string>("SetGreeting", SetGreetingCallback);
                    await hubConnection.Start(new LongPollingTransport());

                    StaticComponents.IsLemuriaHubConnected = await LemurianHub.Invoke<bool>("LemurianSignal", "motor");
                    StaticComponents.LemuriaSettings = await LemurianHub.Invoke<SettingsModels>("GetLemurianSettings", "motor");
                    status = "Connected";                
                }
                InitSettingsAndEvents();
                LemuriaHubStat.Text = status;
                LemuriaConnect();

                ReconnectLemuria.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                StaticComponents.IsLemuriaHubConnected = false;
                LemuriaHubStat.Text = "Not Connected";
                ReconnectLemuria.Visibility = Visibility.Visible;
            }
        }

        private async void InitHelpers()
        {
            webcam = new WebcamHelper();
            await webcam.InitializeCameraAsync();
        }

        /// <summary>
        /// Called once, when the app is first opened. Initializes Oxford facial recognition.
        /// </summary>
        private async void InitOxford()
        {
            // initializedOxford bool will be set to true when Oxford has finished initialization successfully
            initializedOxford = await OxfordFaceAPIHelper.InitializeOxford();

            // Populates UI grid with whitelisted visitors
            UpdateWhitelistedVisitors();
        }

        #endregion

        private void LemuriaConnect()
        {
            // Motor Driver
            enableInputForwardA.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputBackwardA.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputForwardB.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputBackwardB.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            motorASpeed.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            motorBSpeed.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            // Object Avoidance
            FrontSonar.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            // Facial Recognition
            CaptureButton.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            WhitelistedUsersGridView.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            FindPerson.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            // Floor Detection
            Infra1Detect.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            Infra2Detect.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            Infra3Detect.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            Infra4Detect.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            // Temperature Detection
            GetTemperatureHumidity.IsEnabled = StaticComponents.IsLemuriaHubConnected;
        }

        // Call Backs from SignalR

        // Face detection
        private void SetFaceDetectionCameraCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.FaceDetect = value;
                // Future feature
            }).AsTask();
        }

        // Temperature and humidity
        private void SetTemperatureHumiditySensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.InternalTempAndHum = value;
                GetTemperatureHumidity.IsOn = value;
            }).AsTask();
        }

        // Sonar
        private void SetFrontSonarSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.FrontSonar = value;
                FrontSonar.IsOn = value;
            }).AsTask();
        }

        private void SetBackSonarSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.BackSonar = value;
                // Future feature
            }).AsTask();
        }

        // Infra
        private void SetTopLeftIRSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.TopLeftIR = value;
                Infra2Detect.IsOn = value;
            }).AsTask();
        }
        private void SetTopRightIRSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.TopRightIR = value;
                Infra4Detect.IsOn = value;
            }).AsTask();
        }
        private void SetBottomLeftIRSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                StaticComponents.LemuriaSettings.BottomLeftIR = value;
                Infra1Detect.IsOn = value;
            }).AsTask();
        }
        private void SetBottomRightIRSensorCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
           () =>
           {
               StaticComponents.LemuriaSettings.BottomRightIR = value;
                Infra3Detect.IsOn = value;
           }).AsTask();
        }

        // Motor
        private void SetStartMotorBSpeedCallback(int value)
        {
            StaticComponents.LemuriaSettings.StartMotorSpeedB = value;
        }
        private void SetStartMotorASpeedCallback(int value)
        {
            StaticComponents.LemuriaSettings.StartMotorSpeedA = value;
        }
        private void SetMotorBMaxSpeedCallback(int value)
        {
            StaticComponents.LemuriaSettings.MaxMotorSpeedB = value;
        }
        private void SetMotorAMaxSpeedCallback(int value)
        {
            StaticComponents.LemuriaSettings.MaxMotorSpeedA = value;
        }

        // Youtube
        private void YoutubeMusicCallback(bool play, string id, bool isSearch)
        {
            if (play)
                PlayYoutubeMusic(id, isSearch);
            else StopMusic();
        }

        // Volume
        private async void SetVolumeCallback(int volume)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                SpeakVolume.Value = volume;
            });
        }

        // Greeting 
        private async void SetGreetingCallback(string greeting)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                var weather = await GetWeather("Reading", "UK");
                if (weather != null)
                    TextToRead.Text = "The weather is currently " + weather.main.temp + " degrees and has " + weather.weather.First().description;
                ReadText(TextToRead.Text, false);
            });             
        }

        // Move
        #region Movement Logic
        private void ResetDirection(string direction, bool insist)
        {
            if(insist)
                ReadText("I cant move any further in " + direction + " direction", false);

            if (direction == "forward" || direction == "stop")
                ForwardIndicator.Foreground = StaticComponents.inactiveBrush;
            if (direction == "backward" || direction == "stop")
                BackwardIndicator.Foreground = StaticComponents.inactiveBrush;
            if (direction == "left" || direction == "stop")
                LeftIndicator.Foreground = StaticComponents.inactiveBrush;
            if (direction == "right" || direction == "stop")
                RightIndicator.Foreground = StaticComponents.inactiveBrush;

            motorASpeed.Value = 0;
            motorBSpeed.Value = 0;
            enableInputForwardA.IsOn = false;
            enableInputForwardB.IsOn = false;
        }

        private void MoveForward(int forward)
        {
            if(StaticComponents.LemuriaSettings.MaxMotorSpeedA > forward
                & StaticComponents.LemuriaSettings.StartMotorSpeedA < forward)
                motorASpeed.Value = forward;
            if (StaticComponents.LemuriaSettings.MaxMotorSpeedB > forward
                & StaticComponents.LemuriaSettings.StartMotorSpeedB < forward)
                motorBSpeed.Value = forward;
            if (forward > 0)
            {
                enableInputForwardA.IsOn = true;
                enableInputForwardB.IsOn = true;
                ForwardIndicator.Foreground = StaticComponents.activeBrush;
            }
            else ResetDirection("forward", false);
        }

        private void MoveBackward(int backward)
        {
            if (StaticComponents.LemuriaSettings.MaxMotorSpeedA > backward
                & StaticComponents.LemuriaSettings.StartMotorSpeedA < backward)
                motorASpeed.Value = backward;
            if (StaticComponents.LemuriaSettings.MaxMotorSpeedB > backward
                & StaticComponents.LemuriaSettings.StartMotorSpeedB < backward)
                motorBSpeed.Value = backward;
            if (backward > 0)
            {
                enableInputBackwardA.IsOn = true;
                enableInputBackwardB.IsOn = true;
                BackwardIndicator.Foreground = StaticComponents.activeBrush;
            }
            else ResetDirection("backward", false);
        }

        private void TurnLeft(int left)
        {
            if (StaticComponents.LemuriaSettings.MaxMotorSpeedA > left
                & StaticComponents.LemuriaSettings.StartMotorSpeedA < left)
                motorASpeed.Value = left;
            if (left > 0)
            {
                enableInputForwardA.IsOn = true;
                LeftIndicator.Foreground = StaticComponents.activeBrush;
            }
            else ResetDirection("left", false);
        }

        private void TurnRight(int right)
        {
            if (StaticComponents.LemuriaSettings.MaxMotorSpeedB > right
                & StaticComponents.LemuriaSettings.StartMotorSpeedB < right)
                motorBSpeed.Value = right;
            if (right > 0)
            {
                enableInputForwardB.IsOn = true;
                RightIndicator.Foreground = StaticComponents.activeBrush;
            }
            else ResetDirection("right", false);
        }


        private async Task<int> IRNotifier(string update, string method, int value)
        {
            if (update.ToLower() == "false")
            {
                value = 0;
                await LemurianHub.Invoke(method, true);
            }
            else await LemurianHub.Invoke(method, false);
            return value;
        }

        private async Task<int> IRStatusNotifier(List<string> sensors, int value)
        {
            foreach (var sensor in sensors)
            {
                if (sensor == "TopLeft")
                    value = await IRNotifier(infraUpdate.InfraTopLeft.ToString(), "NotifyTopLeftIR", value);
                else if (sensor == "TopRight")
                    value = await IRNotifier(infraUpdate.InfraTopRight.ToString(), "NotifyTopRightIR", value);
                else if (sensor == "BottomLeft")
                    value = await IRNotifier(infraUpdate.InfraBottomLeft.ToString(), "NotifyBottomLeftIR", value);
                else if (sensor == "BottomRight")
                    value = await IRNotifier(infraUpdate.InfraBottomRight.ToString(), "NotifyBottomRightIR", value);
            }
            return value;
        }

        private async Task<int> SensorPass(int value, string direction)
        {
            bool frontSensors = false, backSensors = false, leftSensors = false, rightSensors = false;
            if (StaticComponents.LemuriaSettings.TopLeftIR & StaticComponents.LemuriaSettings.TopRightIR)
            {
                frontSensors = true;
                leftSensors = true;
                rightSensors = true;
            }
            if (StaticComponents.LemuriaSettings.BottomLeftIR & StaticComponents.LemuriaSettings.BottomRightIR)
                backSensors = true;
                

            if (frontSensors & direction == "forward")
                value = await IRStatusNotifier(new List<string> { "TopLeft", "TopRight" }, value);

            if (backSensors & direction == "backward")
                value = await IRStatusNotifier(new List<string> { "BottomLeft", "BottomRight" }, value);

            if (leftSensors & direction == "left")
                value = await IRStatusNotifier(new List<string> { "TopLeft", "TopRight" }, value);

            if (rightSensors & direction == "right")
                value = await IRStatusNotifier(new List<string> { "TopLeft", "TopRight" }, value);

            return value;
        }
        #endregion
        private void MoveRobotCallback(string direction, int forward, int backward, int left, int right)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                switch (direction)
                {
                    case "forward":
                        if (infraUpdate.FrontSonarDistance > 50)
                            MoveForward(await SensorPass(forward, "forward"));
                        else ResetDirection("forward", true);
                        break;
                    case "backward":
                        MoveBackward(await SensorPass(backward, "backward"));
                        break;
                    case "left":
                        if (infraUpdate.FrontSonarDistance > 50)
                            TurnLeft(await SensorPass(left, "left"));
                        else ResetDirection("left", true);
                        break;
                    case "right":
                        if (infraUpdate.FrontSonarDistance > 50)
                            TurnRight(await SensorPass(right, "right"));
                        else ResetDirection("right", true);
                        break;
                    case "stop":
                        ResetDirection(direction, false);
                        break;
                    default:
                        break;
                }
            }).AsTask();
        }
        

        // UI Events

        #region Motor Driver

        private void TurnOffMotorA()
        {
            enableInputForwardA.IsOn = false;
            enableInputBackwardA.IsOn = false;
            StaticMethods.enableMotorA(true, false, enableInputForwardA.IsOn);
            StaticMethods.enableMotorA(false, true, enableInputBackwardA.IsOn);
            ForwardIndicator.Foreground = StaticComponents.inactiveBrush;
            BackwardIndicator.Foreground = StaticComponents.inactiveBrush;
        }

        private void TurnOffMotorB()
        {
            enableInputForwardB.IsOn = false;
            enableInputBackwardB.IsOn = false;
            StaticMethods.enableMotorB(true, false, enableInputForwardB.IsOn);
            StaticMethods.enableMotorB(false, true, enableInputBackwardB.IsOn);
            LeftIndicator.Foreground = StaticComponents.inactiveBrush;
            RightIndicator.Foreground = StaticComponents.inactiveBrush;
        }

        private void motorASpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                StaticComponents.speedAPin.SetActiveDutyCyclePercentage(e.NewValue * .01);
            }
            catch (Exception)
            {
               
            }
        }

        private void motorBSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                StaticComponents.speedBPin.SetActiveDutyCyclePercentage(e.NewValue * .01);
            }
            catch (Exception)
            {

            }
        }

        private void enableInputForwardA_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputForwardA.IsOn)
            {
                enableInputBackwardA.IsOn = false;
                StaticMethods.enableMotorA(true, false, enableInputForwardA.IsOn);
            }
            else TurnOffMotorA();
        }

        private void enableInputBackwardA_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputBackwardA.IsOn)
            {
                enableInputForwardA.IsOn = false;
                StaticMethods.enableMotorA(false, true, enableInputBackwardA.IsOn);
            }
            else TurnOffMotorA();
        }

        private void enableInputForwardB_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputForwardB.IsOn)
            {
                enableInputBackwardB.IsOn = false;
                StaticMethods.enableMotorB(true, false, enableInputForwardB.IsOn);
            }
            else TurnOffMotorB();
        }

        private void enableInputBackwardB_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputBackwardB.IsOn)
            {
                enableInputForwardB.IsOn = false;
                StaticMethods.enableMotorB(false, true, enableInputBackwardB.IsOn);
            }
            else TurnOffMotorB();
        }

        #endregion

        #region Object Avoidance

        private async void FrontSonar_Toggled(object sender, RoutedEventArgs e)
        {
            while (FrontSonar.IsOn)
            {
                SonarStatusText.Text = "";
                try
                {
                    // Your UI update code goes here!
                    await StaticMethods.SendPulse();
                    var distance = StaticMethods.ReceivePulse();

                    infraUpdate.FrontSonarDistance = distance;

                    if (distance > 2 && distance < 30)         //Check whether the distance is within range
                        SonarLog.Text = distance + " cm";      //Print distance with 0.5 cm calibration
                    else
                        SonarLog.Text = "Out Of Range";        //display out of range

                    await LemurianHub.Invoke("NotifySonarDistance", distance);
                }
                catch (Exception)
                {
                    SonarStatusText.Text = "Sonar has error";
                }
                await Task.Delay(StaticComponents.ObjectAvoidanceSensorDelay);
            }

            if (!FrontSonar.IsOn)
            {
                infraUpdate.FrontSonarDistance = 500;
                SonarLog.Text = "Distance";
            }
            SonarStatusText.Text = "";
        }

        #endregion

        #region Facial Recognition

        private void ResetCapture()
        {
            CaptureStatus.Text = "Character";

            // Collapse the confirm photo buttons and open the capture photo button.
            CapturePanel.Visibility = Visibility.Collapsed;
            CaptureButton.Visibility = Visibility.Visible;

            // Collapse the photo control:
            CaptureImage.Visibility = Visibility.Collapsed;
            CaptureImageText.Visibility = Visibility.Visible;
        }

        private async void ConfirmCapture_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CaptureName.Text))
            {
                CaptureStatus.Text = "Saving";
                CaptureName.BorderBrush = new SolidColorBrush(Colors.Black);
                // Create or open the folder in which the Whitelist is stored
                StorageFolder whitelistFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(StaticComponents.WhiteListFolderName, CreationCollisionOption.OpenIfExists);
                // Create a folder to store this specific user's photos
                StorageFolder currentFolder = await whitelistFolder.CreateFolderAsync(CaptureName.Text, CreationCollisionOption.ReplaceExisting);
                // Move the already captured photo the user's folder
                await currentIdPhotoFile.MoveAsync(currentFolder);

                // Add user to Oxford database
                OxfordFaceAPIHelper.AddUserToWhitelist(CaptureName.Text, currentFolder);

                // Photo stream
                var photoStream = await currentIdPhotoFile.OpenAsync(FileAccessMode.Read);
                BitmapImage visitorImage = new BitmapImage();
                await visitorImage.SetSourceAsync(photoStream);

                WhiteListedViewModels.WhitelistedVisitors.Add(new Visitor(CaptureName.Text, currentFolder, visitorImage));

                // Navigate back to MainPage
                ResetCapture();
            }
            else CaptureName.BorderBrush = new SolidColorBrush(Colors.DarkMagenta);
        }

        private void CancelCapture_Click(object sender, RoutedEventArgs e)
        {
            ResetCapture();
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the capture photo button
            CaptureButton.Visibility = Visibility.Collapsed;
            CaptureImageText.Text = "";

            // Capture current frame from webcam, store it in temporary storage and set the source of a BitmapImage to said photo
            currentIdPhotoFile = await webcam.CapturePhoto();
            using (var photoStream = await currentIdPhotoFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapImage idPhotoImage = new BitmapImage();
                await idPhotoImage.SetSourceAsync(photoStream);

                // Set the soruce of the photo control the new BitmapImage and make the photo control visible
                CaptureImage.Source = idPhotoImage;
                CaptureImageText.Visibility = Visibility.Collapsed;
                CaptureImage.Visibility = Visibility.Visible;
                CapturePanel.Visibility = Visibility.Visible;
                CaptureImageText.Text = "";
            }
        }


        /// <summary>
        /// Updates internal list of of whitelisted visitors (whitelistedVisitors) and the visible UI grid
        /// </summary>
        private async void UpdateWhitelistedVisitors()
        {
            // If the whitelist isn't already being updated, update the whitelist
            if (!currentlyUpdatingWhitelist)
            {
                currentlyUpdatingWhitelist = true;
                await UpdateWhitelistedVisitorsList();
                currentlyUpdatingWhitelist = false;
            }
        }

        /// <summary>
        /// Updates the list of Visitor objects with all whitelisted visitors stored on disk
        /// </summary>
        private async Task UpdateWhitelistedVisitorsList()
        {
            // Clears whitelist
            WhiteListedViewModels.WhitelistedVisitors.Clear();

            // If the whitelistFolder has not been opened, open it
            if (whitelistFolder == null)
            {
                whitelistFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(StaticComponents.WhiteListFolderName, CreationCollisionOption.OpenIfExists);
            }

            // Populates subFolders list with all sub folders within the whitelist folders.
            // Each of these sub folders represents the Id photos for a single visitor.
            var subFolders = await whitelistFolder.GetFoldersAsync();

            // Iterate all subfolders in whitelist
            foreach (StorageFolder folder in subFolders)
            {
                string visitorName = folder.Name;
                var filesInFolder = await folder.GetFilesAsync();

                var photoStream = await filesInFolder.Last().OpenAsync(FileAccessMode.Read);
                BitmapImage visitorImage = new BitmapImage();
                await visitorImage.SetSourceAsync(photoStream);

                Visitor whitelistedVisitor = new Visitor(visitorName, folder, visitorImage);

                WhiteListedViewModels.WhitelistedVisitors.Add(whitelistedVisitor);
            }

            // Hide Oxford loading ring
            OxfordLoadingRing.Visibility = Visibility.Collapsed;
        }

        private async void WhitelistedUsersGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedCaptureItem = e.ClickedItem as Visitor;

            var dialog = new ContentDialog()
            {
                Title = selectedCaptureItem.Name,
                //RequestedTheme = ElementTheme.Dark,
                //FullSizeDesired = true,
                MaxWidth = this.ActualWidth // Required for Mobile!
            };

            // Setup Content
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = "Please select one of the options",
                TextWrapping = TextWrapping.Wrap,
            });


            dialog.Content = panel;

            // Add Buttons
            dialog.PrimaryButtonText = "Add Photo";
            dialog.PrimaryButtonClick += async delegate
            {
                WhitelistedUsersGridView.Visibility = Visibility.Collapsed;
                OxfordLoadingRing.Visibility = Visibility.Visible;
                // Captures photo from current webcam stream
                StorageFile imageFile = await webcam.CapturePhoto();
                // Moves the captured file to the current user's ID image folder
                await imageFile.MoveAsync(selectedCaptureItem.ImageFolder);
                // Update image preview with the latest image
                var photoStream = await imageFile.OpenAsync(FileAccessMode.Read);
                BitmapImage visitorImage = new BitmapImage();
                await visitorImage.SetSourceAsync(photoStream);
                selectedCaptureItem.Image = visitorImage;
                // Add to Oxford
                OxfordFaceAPIHelper.AddImageToWhitelist(imageFile, selectedCaptureItem.Name);
                OxfordLoadingRing.Visibility = Visibility.Collapsed;
                WhitelistedUsersGridView.Visibility = Visibility.Visible;
            };

            dialog.SecondaryButtonText = "Delete Profile";
            dialog.SecondaryButtonClick += async delegate
            {
                // Delete the user's folder
                await selectedCaptureItem.ImageFolder.DeleteAsync();
                // Remove the Captured Item
                WhiteListedViewModels.WhitelistedVisitors.Remove(selectedCaptureItem);
                // Remove user from Oxford
                OxfordFaceAPIHelper.RemoveUserFromWhitelist(selectedCaptureItem.Name);
            };

            

            // Show Dialog
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None)
            {
                dialog.Hide();
            }
        }

        private async void FindPerson_Click(object sender, RoutedEventArgs e)
        {
            // Hide the clicked button
            FindPerson.Visibility = Visibility.Collapsed;
            FoundPerson.Text = "Finding...";
            // List to store visitors recognized by Oxford Face API
            // Count will be greater than 0 if there is an authorized visitor at the door
            List<string> recognizedVisitors = new List<string>();

            // Confirms that webcam has been properly initialized and oxford is ready to go
            if (webcam.IsInitialized() && initializedOxford)
            {
                // Stores current frame from webcam feed in a temporary folder
                StorageFile image = await webcam.CapturePhoto();

                try
                {
                    // Oxford determines whether or not the visitor is on the Whitelist and returns true if so
                    recognizedVisitors = await OxfordFaceAPIHelper.IsFaceInWhitelist(image);
                }
                catch (FaceRecognitionException fe)
                {
                    switch (fe.ExceptionType)
                    {
                        // Fails and catches as a FaceRecognitionException if no face is detected in the image
                        case FaceRecognitionExceptionType.NoFaceDetected:
                            FoundPerson.Text = "WARNING: No face detected in this image.";
                            break;
                    }
                }
                catch (FaceAPIException faceAPIEx)
                {
                    Debug.WriteLine("FaceAPIException in IsFaceInWhitelist(): " + faceAPIEx.ErrorMessage);
                }
                catch
                {
                    // General error. This can happen if there are no visitors authorized in the whitelist
                    Debug.WriteLine("WARNING: Oxford just threw a general expception.");
                }

                if (recognizedVisitors.Count > 0)
                {
                    FoundPerson.Text = "Hello,";
                    // If everything went well and a visitor was recognized, unlock the door:
                    foreach (var recognisedVisitor in recognizedVisitors)
                    {
                        FoundPerson.Text += " " + recognisedVisitor;
                    }
                }
                else
                {
                    FoundPerson.Text = "Not recognised";
                }
            }
            else
            {
                if (!webcam.IsInitialized())
                {
                    // The webcam has not been fully initialized for whatever reason:
                    Debug.WriteLine("Unable to analyze visitor at door as the camera failed to initlialize properly.");
                }

                if (!initializedOxford)
                {
                    // Oxford is still initializing:
                    Debug.WriteLine("Unable to analyze visitor at door as Oxford Facial Recogntion is still initializing.");
                }
            }

            FindPerson.Visibility = Visibility.Visible;
        }

        #endregion

        #region Floor Detection

        private bool HasInfra1Obstacle()
        {
            bool hasObject = false;

            try
            {
                if (StaticComponents.infra1Pin.Read() == GpioPinValue.High)
                    hasObject = true;
            }
            catch (Exception)
            {
                InfraStatusText.Text = "Infra 1 has error";
            }

            return hasObject;
        }

        private bool HasInfra2Obstacle()
        {
            bool hasObject = false;

            try
            {
                if (StaticComponents.infra2Pin.Read() == GpioPinValue.High)
                    hasObject = true;
            }
            catch (Exception)
            {
                InfraStatusText.Text = "Infra 2 has error";
            }

            return hasObject;
        }

        private bool HasInfra3Obstacle()
        {
            bool hasObject = false;

            try
            {
                if (StaticComponents.infra3Pin.Read() == GpioPinValue.High)
                    hasObject = true;
            }
            catch (Exception)
            {
                InfraStatusText.Text = "Infra 3 has error";
            }

            return hasObject;
        }

        private bool HasInfra4Obstacle()
        {
            bool hasObject = false;

            try
            {
                if (StaticComponents.infra4Pin.Read() == GpioPinValue.High)
                    hasObject = true;
            }
            catch (Exception)
            {
                InfraStatusText.Text = "Infra 4 has error";
            }


            return hasObject;
        }

        private async void Infra1Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra1Detect.IsOn)
            {
                InfraStatusText.Text = "";
                if (!HasInfra1Obstacle())
                {
                    Infra1Detected.Text = "Obstacle Detected";
                    infraUpdate.InfraBottomLeft = true;
                }
                else
                {
                    Infra1Detected.Text = "No Obstacle";
                    infraUpdate.InfraBottomLeft = false;
                }

                await Task.Delay(StaticComponents.FloorDetectionSensorDelay);
            }
            InfraStatusText.Text = "";
        }

        private async void Infra2Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra2Detect.IsOn)
            {
                InfraStatusText.Text = "";
                if (!HasInfra2Obstacle())
                {
                    Infra2Detected.Text = "Obstacle Detected";
                    infraUpdate.InfraTopLeft = true;
                }
                else
                {
                    Infra2Detected.Text = "No Obstacle";
                    infraUpdate.InfraTopLeft = false;
                }

                await Task.Delay(StaticComponents.FloorDetectionSensorDelay);
            }
            InfraStatusText.Text = "";
        }

        private async void Infra3Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra3Detect.IsOn)
            {
                InfraStatusText.Text = "";
                if (!HasInfra3Obstacle())
                {
                    Infra3Detected.Text = "Obstacle Detected";
                    infraUpdate.InfraBottomRight = true;
                }
                else
                {
                    Infra3Detected.Text = "No Obstacle";
                    infraUpdate.InfraBottomRight = false;
                }

                await Task.Delay(StaticComponents.FloorDetectionSensorDelay);
            }
            InfraStatusText.Text = "";
        }

        private async void Infra4Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra4Detect.IsOn)
            {
                InfraStatusText.Text = "";
                if (!HasInfra4Obstacle())
                {
                    Infra4Detected.Text = "Obstacle Detected";
                    infraUpdate.InfraTopRight = true;
                }
                else
                {
                    Infra4Detected.Text = "No Obstacle";
                    infraUpdate.InfraTopRight = false;
                }

                await Task.Delay(StaticComponents.FloorDetectionSensorDelay);
            }
            InfraStatusText.Text = "";
        }

        #endregion

        #region Temperature Detection

        private async Task<Tuple<double, double>> GetTemperatureAndHumidity()
        {
            double temperature = 0, humidity = 0;

            try
            {
                DhtReading reading = new DhtReading();
                reading = await _dhtInterface.GetReadingAsync();

                if (reading.IsValid)
                {
                    temperature = Convert.ToSingle(reading.Temperature);
                    humidity = Convert.ToSingle(reading.Humidity);

                    await LemurianHub.Invoke("NotifyTemperatureHumidity", new object[] { temperature, humidity });
                }
                else
                {
                    Debug.WriteLine("Temperature & Humidity Sensor: Reading not valid");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Temperature & Humidity Sensor: " + ex.Message);
            }


            return new Tuple<double, double>(temperature, humidity);
        }

        private async void GetTemperatureHumidity_Toggled(object sender, RoutedEventArgs e)
        {
            while (GetTemperatureHumidity.IsOn)
            {
                TempHumStatusText.Text = "";
                try
                {
                    var tempHum = await GetTemperatureAndHumidity();
                    TemperatureText.Text = tempHum.Item1.ToString() + " °C";
                    HumidityText.Text = tempHum.Item2.ToString() + " RH";
                }
                catch (Exception)
                {
                    TempHumStatusText.Text = "Temperature and humidity sensor has error";
                }

                await Task.Delay(StaticComponents.TempHumSensorDelay);
            }
        }

        #endregion

        #region Speakers
        private async void ReadText(string text, bool backlog)
        {
            if (mediaElement.CurrentState != MediaElementState.Playing)
            {
                SpeechSynthesisStream voiceStream = await speechSynthesizer.SynthesizeTextToStreamAsync(text);
                voiceStream.Seek(0);
                mediaElement.SetSource(voiceStream, voiceStream.ContentType);
                mediaElement.Play();
            }
            else
            {
                if (backlog)
                    textReadback.Add(text);
            }
        }

        private void MediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            if (mediaElement.CurrentState == MediaElementState.Stopped)
            {
                if (textReadback.Count > 0)
                {
                    ReadText(textReadback[0], false);
                    textReadback.RemoveAt(0);
                }
            }
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            UpdateSpeakerStatus();
        }

        private async void UpdateSpeakerStatus()
        {
            var id = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Communications);
            if (!string.IsNullOrEmpty(id))
            {
                var deviceInfo = await DeviceInformation.CreateFromIdAsync(id);
                ReadingStatus.Text = deviceInfo.Name;
                ReadClick.IsEnabled = true;
                SpeakVolume.Visibility = Visibility.Visible;
                mediaElement.Volume = 0.30;
                SpeakVolume.Value = mediaElement.Volume * 100;
            }
            else
            {
                ReadClick.IsEnabled = false;
                SpeakVolume.Visibility = Visibility.Collapsed;
                ReadingStatus.Text = "No audio device detected";
            }
        }

        private void SpeakVolume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            mediaElement.Volume = SpeakVolume.Value/100;
        }

        private void ReadClick_Click(object sender, RoutedEventArgs e)
        {
            ReadText(TextToRead.Text, false);
        }


        private void DefaultReadClick_Click(object sender, RoutedEventArgs e)
        {
            TextToRead.Text = "I am 'B cube Box Head' and I am loving it!";
        }

        private async void WeatherReadClick_Click(object sender, RoutedEventArgs e)
        {
            var weather = await GetWeather("Reading", "UK");
            if (weather != null)
                TextToRead.Text = "The weather is currently " + weather.main.temp + " degrees and has " + weather.weather.First().description;
        }

        private async void YoutubeReadClick_Click(object sender, RoutedEventArgs e)
        {
            var youtube = await GetYoutubeTrending(10, "GB");
            if (youtube != null)
            {
                var text = "Top trending music videos in UK are ";
                for (int i = 0; i < youtube.items.Count; i++)
                {
                    var punctuation = "";
                    if (youtube.items.Count == i + 2)
                        punctuation = " and ";
                    else if (youtube.items.Count == i + 1)
                        punctuation = ".";
                    else punctuation = ", ";

                    var item = youtube.items[i];
                    text += item.snippet.title + punctuation;
                }

                TextToRead.Text = text;
            }
        }

        private async Task<bool> PlayYoutube(string youtubeId)
        {
            bool status = false;
            PlayMusicClick.Visibility = Visibility.Collapsed;
            if (youtubeId != null)
            {
                var youtube = YouTube.Default;
                var videosAudios = await youtube.GetAllVideosAsync("https://www.youtube.com/watch?v=" + youtubeId);
                foreach (var videoAudio in videosAudios)
                {
                    if (videoAudio.AdaptiveKind == AdaptiveKind.Audio & videoAudio.Format == VideoFormat.Mp4)
                    {
                        try
                        {
                            PlayMusicClick.Content = "";
                            TextToRead.Text = "Loading " + videoAudio.Title;
                            var memStream = new MemoryStream();
                            await videoAudio.Stream().CopyToAsync(memStream);
                            memStream.Seek(0, SeekOrigin.Begin);
                            mediaElement.SetSource(memStream.AsRandomAccessStream(), "");
                            TextToRead.Text = videoAudio.Title;
                            mediaElement.Play();
                            status = true;
                            break;
                        }
                        catch (Exception)
                        {
                            TextToRead.Text = "Sorry can't play this link";
                        }
                    }
                    else TextToRead.Text = "Sorry can't play this link";
                }
            }
            else TextToRead.Text = "Sorry can't play this link";
            PlayMusicClick.Visibility = Visibility.Visible;
            return status;
        }

        private async void PlayYoutubeMusic(string youtubeId, bool isSearch)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                if (isSearch)
                {
                    if (youtubeId.ToLower() == "top trending music")
                    {
                        var youtube = await GetYoutubeTrending(10, "GB");
                        if (youtube != null)
                        {
                            foreach (var item in youtube.items)
                            {
                                TextToRead.Text = "Trying " + item.snippet.title;
                                var isPlaying = await PlayYoutube(item.id);
                                if (isPlaying) break;
                            }
                        }
                    }
                    else
                    {
                        var youtube = await GetYoutubeSearch(youtubeId);
                        if (youtube != null)
                        {
                            foreach (var item in youtube.items)
                            {
                                TextToRead.Text = "Trying " + item.snippet.title;
                                var isPlaying = await PlayYoutube(item.id.videoId);
                                if (isPlaying) break;
                            }
                        }
                    }
                }
                else await PlayYoutube(youtubeId);
            });
        }

        private async void StopMusic()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                PlayMusicClick.Content = "";
                TextToRead.Text = "Stopped music";
                mediaElement.Stop();
            });
        }

        private void PlayMusicClick_Click(object sender, RoutedEventArgs e)
        {
            if (PlayMusicClick.Content.ToString() == "")
                PlayYoutubeMusic("Michael Bublé - I Believe in You", true);
            else
                StopMusic();
        }
        #endregion

        // Screen Saver
        private void ScreenSaver_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ScreenSaver.Visibility = Visibility.Collapsed;
            ContentGrid.Visibility = Visibility.Visible;
        }

        private void ContentToScreenSaver_Click(object sender, RoutedEventArgs e)
        {
            ScreenSaver.Visibility = Visibility.Visible;
            ContentGrid.Visibility = Visibility.Collapsed;
        }

        // Reconnect
        private async void ReconnectLemuria_Click(object sender, RoutedEventArgs e)
        {
            ReconnectLemuria.Visibility = Visibility.Collapsed;
            LemuriaHubStat.Text = "Loading";
            await Task.Delay(2000);
            InitHub();
        }

        // Common information
        private async Task<WeatherModels> GetWeather(string city, string country)
        {
            WeatherModels weather = null;
            try
            {
                using (var client = new HttpClient())
                {
                    //HTTP get
                    var url = StaticComponents.OpenWeatherURL + "&q=" + WebUtility.UrlEncode(city) + "," + WebUtility.UrlEncode(country) + StaticComponents.OpenWeatherAPIKey;
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync().Result;
                        weather = JsonConvert.DeserializeObject<WeatherModels>(jsonString);
                    }
                }
            }
            catch (Exception)
            {
                weather = null;
            }

            return weather;
        }

        private async Task<YoutubeMostPopularModels> GetYoutubeTrending(int videoCategoryID, string country)
        {
            YoutubeMostPopularModels youtube = null;
            try
            {
                using (var client = new HttpClient())
                {
                    //HTTP get
                    var url = StaticComponents.YoutubeMostPopularURL + "&videoCategoryId=" + videoCategoryID + "&regionCode=" + WebUtility.UrlEncode(country) + StaticComponents.YoutubeAPIKey;
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync().Result;
                        youtube = JsonConvert.DeserializeObject<YoutubeMostPopularModels>(jsonString);
                    }
                }
            }
            catch (Exception)
            {
                youtube = null;
            }

            return youtube;
        }

        private async Task<YoutubeSearchModels> GetYoutubeSearch(string search)
        {
            YoutubeSearchModels youtube = null;
            try
            {
                using (var client = new HttpClient())
                {
                    //HTTP get
                    var url = StaticComponents.YoutubeSearchURL + "&q=" + WebUtility.UrlEncode(search) + StaticComponents.YoutubeAPIKey;
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = response.Content.ReadAsStringAsync().Result;
                        youtube = JsonConvert.DeserializeObject<YoutubeSearchModels>(jsonString);
                    }
                }
            }
            catch (Exception)
            {
                youtube = null;
            }

            return youtube;
        }
    }
}

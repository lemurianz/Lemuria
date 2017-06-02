using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using ShastaController.uwp.Helpers;
using ShastaController.uwp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
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

namespace ShastaController.uwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private LEDStripLight ledStripLights { get; set; }
        public MainPageNotifiers notifiers { get; set; }
        public IHubProxy LemurianHub { get; set; }
        public MotorSpeedModels speed { get; set; }
        public int PivotSize { get; set; }
        int previousKey = -1;
        public MainPage()
        {
            this.InitializeComponent();
            SelectPage("loading");
            ledStripLights = new LEDStripLight();
            notifiers = new MainPageNotifiers();
            speed = new MotorSpeedModels();

            ApplicationView.PreferredLaunchViewSize = new Size(700, 450);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

        }

        private void MainPage_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            ResetAllSpeedNotifiers("");
            previousKey = -1;
        }

        private void MainPage_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            // For some reason holding the keyboard key down triggers this event multiple times
            if (previousKey != (int)args.VirtualKey)
            {
                switch (args.VirtualKey)
                {
                    case Windows.System.VirtualKey.Up:
                    case Windows.System.VirtualKey.W:
                    case Windows.System.VirtualKey.GamepadDPadUp:
                    case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
                        ResetAllSpeedNotifiers("forward");
                        break;

                    case Windows.System.VirtualKey.Down:
                    case Windows.System.VirtualKey.S:
                    case Windows.System.VirtualKey.GamepadDPadDown:
                    case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
                        ResetAllSpeedNotifiers("backward");
                        break;

                    case Windows.System.VirtualKey.Left:
                    case Windows.System.VirtualKey.A:
                    case Windows.System.VirtualKey.GamepadDPadLeft:
                    case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
                        ResetAllSpeedNotifiers("left");
                        break;

                    case Windows.System.VirtualKey.Right:
                    case Windows.System.VirtualKey.D:
                    case Windows.System.VirtualKey.GamepadDPadRight:
                    case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
                        ResetAllSpeedNotifiers("right");
                        break;

                    case Windows.System.VirtualKey.GamepadLeftTrigger:
                        if(LemuriaPivot.SelectedIndex > 0) LemuriaPivot.SelectedIndex -= 1;
                        break;

                    case Windows.System.VirtualKey.GamepadRightTrigger:
                        if (LemuriaPivot.SelectedIndex < PivotSize -1) LemuriaPivot.SelectedIndex += 1;
                        break;

                    case Windows.System.VirtualKey.GamepadY:
                        if(SpeechButton.IsEnabled & SpeechButton.Visibility == Visibility.Visible) RecordSpeech(true);
                        break;

                    default:
                        break;
                }
                previousKey = (int)args.VirtualKey;
            }
        }

        async void TryConnectToLEDStrips()
        {
            IRTopLeftTextblock.Text = "Connecting";
            await ledStripLights.ConnectAsync("192.168.0.59", "5000");
            IRTopLeftTextblock.Text = "Connected";
        }

        async Task<bool> TryConnectToLemuria()
        {
            var hasConnected = false;
            try
            {
                var hubConnection = new HubConnection("http://localhost:52232/");
                LemurianHub = hubConnection.CreateHubProxy("LemuriaHub");
                LemurianHub.On<bool>("NotifyTopLeftIR", NotifyTopLeftIRCallback);
                LemurianHub.On<bool>("NotifyTopRightIR", NotifyTopRightIRCallback);
                LemurianHub.On<bool>("NotifyBottomLeftIR", NotifyBottomLeftIRCallback);
                LemurianHub.On<bool>("NotifyBottomRightIR", NotifyBottomRightIRCallback);
                LemurianHub.On<double>("NotifySonarDistance", NotifySonarDistanceCallback);
                LemurianHub.On<double, double>("NotifyTemperatureHumidity", NotifyTemperatureHumidityCallback);
                await hubConnection.Start(new LongPollingTransport());

                // Start the Lemuria
                hasConnected = await LemurianHub.Invoke<bool>("LemurianSignal", "master");
            }
            catch (Exception)
            {
                hasConnected = false;
            }


            return hasConnected;
        }

        // Callbacks
        private void NotifyBottomRightIRCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value)
                    IRBottomRightElipse.Fill = new SolidColorBrush(Colors.Red);
                else IRBottomRightElipse.Fill = new SolidColorBrush(Colors.White);
            }).AsTask();
        }

        private void NotifyBottomLeftIRCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value)
                    IRBottomLeftElipse.Fill = new SolidColorBrush(Colors.Red);
                else IRBottomLeftElipse.Fill = new SolidColorBrush(Colors.White);
            }).AsTask();
        }

        private void NotifyTopRightIRCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value)
                    IRTopRightElipse.Fill = new SolidColorBrush(Colors.Red);
                else IRTopRightElipse.Fill = new SolidColorBrush(Colors.White);
            }).AsTask();

        }

        private void NotifyTopLeftIRCallback(bool value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value)
                    IRTopLeftElipse.Fill = new SolidColorBrush(Colors.Red);
                else IRTopLeftElipse.Fill = new SolidColorBrush(Colors.White);
            }).AsTask();
        }

        private void NotifySonarDistanceCallback(double value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value == 0)
                    DistanceTextblock.Text = "Distance";
                else DistanceTextblock.Text = value.ToString();
            }).AsTask();
        }

        private void NotifyTemperatureHumidityCallback(double value1, double value2)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (value1 == 0)
                    TemperatureTextblock.Text = "Temperature";
                else TemperatureTextblock.Text = value1.ToString() + " °C";

                if (value2 == 0)
                    HumidityTextblock.Text = "Humidity";
                else HumidityTextblock.Text = value2.ToString() + " RH";
            }).AsTask();
        }

        void SelectPage(string page)
        {
            if (page == "main")
                LemuriaPivot.Visibility = Visibility.Visible;
            else LemuriaPivot.Visibility = Visibility.Collapsed;

            if (page == "connect")
                LemuriaConnect.Visibility = Visibility.Visible;
            else LemuriaConnect.Visibility = Visibility.Collapsed;

            if (page == "loading")
                LemuriaLoading.Visibility = Visibility.Visible;
            else LemuriaLoading.Visibility = Visibility.Collapsed;

        }

        async Task Initialize()
        {
            PivotSize = LemuriaPivot.Items.Count;
            // load a jpeg, be sure to have the Pictures Library capability in your manifest
            var folder = await Package.Current.InstalledLocation.GetFolderAsync(@"Assets");
            var file = await folder.GetFileAsync("Background.jpg");
            using (var data = await file.OpenAsync(FileAccessMode.Read))
            {

                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(data);
                LemuriaPivot.Background = new ImageBrush { ImageSource = bitmapImage, Stretch = Stretch.UniformToFill };
                // Ensure image loaded
                await Task.Delay(500);
            }

            // Notifiers
            notifiers.PropertyChanged += Notifiers_PropertyChanged;
            // Forward
            ForwardButton.AddHandler(PointerPressedEvent, new PointerEventHandler(MoveForward_PointerPressed), true);
            ForwardButton.AddHandler(PointerReleasedEvent, new PointerEventHandler(MoveForward_PointerReleased), true);
            // Backward
            BackwardButton.AddHandler(PointerPressedEvent, new PointerEventHandler(MoveBackward_PointerPressed), true);
            BackwardButton.AddHandler(PointerReleasedEvent, new PointerEventHandler(MoveBackward_PointerReleased), true);
            // Left
            LeftButoon.AddHandler(PointerPressedEvent, new PointerEventHandler(TurnLeft_PointerPressed), true);
            LeftButoon.AddHandler(PointerReleasedEvent, new PointerEventHandler(TurnLeft_PointerReleased), true);
            // Right
            RightButton.AddHandler(PointerPressedEvent, new PointerEventHandler(TurnRight_PointerPressed), true);
            RightButton.AddHandler(PointerReleasedEvent, new PointerEventHandler(TurnRight_PointerReleased), true);

            SelectPage("connect");
        }

        private void Notifiers_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsForwardPressed")
                MoveForward();
            if (e.PropertyName == "IsBackwardPressed")
                MoveBackward();
            if (e.PropertyName == "IsLeftPressed")
                TurnLeft();
            if (e.PropertyName == "IsRightPressed")
                TurnRight();
        }

        private async void LemuriaPivot_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }

        private void ResetAllSpeeds()
        {
            if (!notifiers.IsLeftPressed)
                speed.LeftSpeed = 0;
            if (!notifiers.IsRightPressed)
                speed.RightSpeed = 0;
            if (!notifiers.IsForwardPressed)
                speed.ForwardSpeed = 0;
            if (!notifiers.IsBackwardPressed)
                speed.BackwardSpeed = 0;

            SpeedTextblock.Text = "Speed";
            DirectionTextblock.Text = "";
        }

        private void ResetAllSpeedNotifiers(string direction)
        {
            // forward
            if (direction == "forward") notifiers.IsForwardPressed = true;
            else notifiers.IsForwardPressed = false;
            // backward
            if (direction == "backward") notifiers.IsBackwardPressed = true;
            else notifiers.IsBackwardPressed = false;
            // left
            if (direction == "left") notifiers.IsLeftPressed = true;
            else notifiers.IsLeftPressed = false;
            // right
            if (direction == "right") notifiers.IsRightPressed = true;
            else notifiers.IsRightPressed = false;
        }

        private int MovementStopped()
        {
            SpeedTextblock.Text = "Speed";
            DirectionTextblock.Text = "";
            MotorATextblock.Text = "◐";
            MotorBTextblock.Text = "◑";
            return 0;
        }

        private async void LemurianMove(string direction, MotorSpeedModels speed)
        {
            // Send this data to the Server
            await LemurianHub.Invoke("LemurianMove", new object[] { direction, speed });
        }

        private async void MoveForward()
        {
            if (notifiers.IsForwardPressed)
            {
                ResetAllSpeeds();
                if (StartMotorASpeed.Value < StartMotorBSpeed.Value)
                    speed.ForwardSpeed = (int)StartMotorASpeed.Value;
                else speed.ForwardSpeed = (int)StartMotorBSpeed.Value;

                while (notifiers.IsForwardPressed)
                {
                    if (speed.ForwardSpeed > MaxMotorASpeed.Value) MotorATextblock.Text = "◈";
                    if (speed.ForwardSpeed > MaxMotorBSpeed.Value) MotorBTextblock.Text = "◈";
                    if (speed.ForwardSpeed < 100)
                    {
                        DirectionTextblock.Text = "⮝";
                        SpeedTextblock.Text = speed.ForwardSpeed.ToString();
                        speed.ForwardSpeed += 1;
                    }
                    await Task.Delay(250);

                    LemurianMove("forward", speed);
                }
            }
            else speed.ForwardSpeed = MovementStopped();
        }

        private async void MoveBackward()
        {
            if (notifiers.IsBackwardPressed)
            {
                ResetAllSpeeds();

                if (StartMotorASpeed.Value < StartMotorBSpeed.Value)
                    speed.BackwardSpeed = (int)StartMotorASpeed.Value;
                else speed.BackwardSpeed = (int)StartMotorBSpeed.Value;
                while (notifiers.IsBackwardPressed)
                {
                    if (speed.BackwardSpeed > MaxMotorASpeed.Value) MotorATextblock.Text = "◈";
                    if (speed.BackwardSpeed > MaxMotorBSpeed.Value) MotorBTextblock.Text = "◈";
                    if (speed.BackwardSpeed < 100)
                    {
                        SpeedTextblock.Text = speed.BackwardSpeed.ToString();
                        DirectionTextblock.Text = "⮟";
                        speed.BackwardSpeed += 1;
                    }
                    await Task.Delay(250);

                    LemurianMove("backward", speed);
                }
            }
            else speed.BackwardSpeed = MovementStopped();
        }

        private async void TurnLeft()
        {
            if (notifiers.IsLeftPressed)
            {
                ResetAllSpeeds();
                while (notifiers.IsLeftPressed)
                {
                    if (speed.LeftSpeed > MaxMotorASpeed.Value) MotorATextblock.Text = "◈";
                    if (speed.LeftSpeed < 100)
                    {
                        SpeedTextblock.Text = speed.LeftSpeed.ToString();
                        DirectionTextblock.Text = "⮜";
                        speed.LeftSpeed += 1;
                    }
                    await Task.Delay(250);

                    LemurianMove("left", speed);
                }
            }
            else speed.LeftSpeed = MovementStopped();
        }

        private async void TurnRight()
        {
            if (notifiers.IsRightPressed)
            {
                ResetAllSpeeds();
                while (notifiers.IsRightPressed)
                {
                    if (speed.RightSpeed > MaxMotorBSpeed.Value) MotorBTextblock.Text = "◈";
                    if (speed.RightSpeed < 100)
                    {
                        SpeedTextblock.Text = speed.RightSpeed.ToString();
                        DirectionTextblock.Text = "⮞";
                        speed.RightSpeed += 1;
                    }
                    await Task.Delay(250);

                    LemurianMove("right", speed);
                }
            }
            else speed.RightSpeed = MovementStopped();
        }

        private void MoveForward_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("forward");
        }

        private void MoveForward_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("");
        }

        private void MoveBackward_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("backward");
        }

        private void MoveBackward_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("");
        }

        private void TurnLeft_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("left");
        }

        private void TurnLeft_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("");
        }

        private void TurnRight_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("right");
        }

        private void TurnRight_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ResetAllSpeedNotifiers("");
        }

        async Task<string> RecordSpeechFromMicrophoneAsync(bool isGamepad)
        {
            string recognizedText = string.Empty;
            try
            {
                using (SpeechRecognizer recognizer = new SpeechRecognizer())
                {
                    await recognizer.CompileConstraintsAsync();

                    SpeechRecognitionResult result = null;

                    if (isGamepad)
                        result = await recognizer.RecognizeAsync();
                    else result = await recognizer.RecognizeWithUIAsync();

                    if (result.Status == SpeechRecognitionResultStatus.Success)
                    {
                        recognizedText = result.Text;
                    }
                }
            }

            // Catch errors related to the recognition operation.
            catch (Exception err)
            {
                // Define a variable that holds the error for the speech recognition privacy policy. 
                // This value maps to the SPERR_SPEECH_PRIVACY_POLICY_NOT_ACCEPTED error, 
                // as described in the Windows.Phone.Speech.Recognition error codes section later on.
                const int privacyPolicyHResult = unchecked((int)0x80045509);

                // Check whether the error is for the speech recognition privacy policy.
                if (err.HResult == privacyPolicyHResult)
                {
                    var dialog = new MessageDialog("You will need to accept the speech privacy policy in order to use speech recognition in this app.");
                    await dialog.ShowAsync();
                }
                else
                {
                    // Handle other types of errors here.
                }
            }
            return (recognizedText);
        }

        private async void RecordSpeech(bool isGamepad)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                SpeechButton.IsEnabled = false;
                string text = await RecordSpeechFromMicrophoneAsync(isGamepad);
                if (!string.IsNullOrEmpty(text))
                {
                    SpeechButton.Visibility = Visibility.Collapsed;
                    SpeechPreviewGrid.Visibility = Visibility.Visible;
                    SpeechPreviewText.Text = text;
                }
                SpeechButton.IsEnabled = true;
            });
        }

        private void SpeechCorrectButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SpeechWrongButton_Click(object sender, RoutedEventArgs e)
        {
            SpeechButton.Visibility = Visibility.Visible;
            SpeechPreviewGrid.Visibility = Visibility.Collapsed;
            SpeechPreviewText.Text = "";
        }

        private void SpeechButton_Click(object sender, RoutedEventArgs e)
        {
            RecordSpeech(false);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (await TryConnectToLemuria())
            {
                SelectPage("main");
                CoreWindow.GetForCurrentThread().KeyDown += MainPage_KeyDown;
                CoreWindow.GetForCurrentThread().KeyUp += MainPage_KeyUp;
                UpdateSettingsPage(await LemurianHub.Invoke<SettingsModels>("GetLemurianSettings", "master"));
            }
        }

        #region Settings
        private void UpdateIRStatus()
        {
            if (TopLeftIRSensor.IsOn)
            {
                IRTopLeftElipse.Fill = new SolidColorBrush(Colors.White);
                IRTopLeftTextblock.Text = "On";
            }
            else
            {
                IRTopLeftElipse.Fill = new SolidColorBrush(Colors.Silver);
                IRTopLeftTextblock.Text = "off";
            }

            if (TopRightIRSensor.IsOn)
            {
                IRTopRightElipse.Fill = new SolidColorBrush(Colors.White);
                IRTopRightTextblock.Text = "On";
            }
            else
            {
                IRTopRightElipse.Fill = new SolidColorBrush(Colors.Silver);
                IRTopRightTextblock.Text = "off";
            }

            if (BottomLeftIRSensor.IsOn)
            {
                IRBottomLeftElipse.Fill = new SolidColorBrush(Colors.White);
                IRBottomLeftTextblock.Text = "On";
            }
            else
            {
                IRBottomLeftElipse.Fill = new SolidColorBrush(Colors.Silver);
                IRBottomLeftTextblock.Text = "off";
            }

            if (BottomRightIRSensor.IsOn)
            {
                IRBottomRightElipse.Fill = new SolidColorBrush(Colors.White);
                IRBottomRightTextblock.Text = "On";
            }
            else
            {
                IRBottomRightElipse.Fill = new SolidColorBrush(Colors.Silver);
                IRBottomRightTextblock.Text = "off";
            }
        }

        private void UpdateSettingsPage(SettingsModels model)
        {
            // Setup values
            //motor
            MaxMotorASpeed.Value = model.MaxMotorSpeedA;
            MaxMotorBSpeed.Value = model.MaxMotorSpeedB;
            //ir sensors
            TopLeftIRSensor.IsOn = model.TopLeftIR;
            TopRightIRSensor.IsOn = model.TopRightIR;
            BottomLeftIRSensor.IsOn = model.BottomLeftIR;
            BottomRightIRSensor.IsOn = model.BottomRightIR;
            //sonar senors
            FrontSonarSensor.IsOn = model.FrontSonar;
            BackSonarSensor.IsOn = model.BackSonar;
            //temperature humidity sensor
            TemperatureHumiditySensor.IsOn = model.InternalTempAndHum;
            //face detection
            FaceDetectionCamera.IsOn = model.FaceDetect;

            // Setup events
            //motor
            StartMotorASpeed.ValueChanged += StartMotorASpeed_ValueChanged;
            StartMotorBSpeed.ValueChanged += StartMotorBSpeed_ValueChanged;
            MaxMotorASpeed.ValueChanged += MaxMotorASpeed_ValueChanged;
            MaxMotorBSpeed.ValueChanged += MaxMotorBSpeed_ValueChanged;
            //ir sensors
            TopLeftIRSensor.Toggled += TopLeftIRSensor_Toggled;
            TopRightIRSensor.Toggled += TopRightIRSensor_Toggled;
            BottomLeftIRSensor.Toggled += BottomLeftIRSensor_Toggled;
            BottomRightIRSensor.Toggled += BottomRightIRSensor_Toggled;
            //sonar sensors
            FrontSonarSensor.Toggled += FrontSonarSensor_Toggled;
            BackSonarSensor.Toggled += BackSonarSensor_Toggled;
            //temperature humidity sensor
            TemperatureHumiditySensor.Toggled += TemperatureHumiditySensor_Toggled;
            //face detection
            FaceDetectionCamera.Toggled += FaceDetectionCamera_Toggled;

            // Initialize
            UpdateIRStatus();
        }

        private async void FaceDetectionCamera_Toggled(object sender, RoutedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetFaceDetectionCamera", ((ToggleSwitch)sender).IsOn);
        }

        private async void TemperatureHumiditySensor_Toggled(object sender, RoutedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetTemperatureHumiditySensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void BackSonarSensor_Toggled(object sender, RoutedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetBackSonarSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void FrontSonarSensor_Toggled(object sender, RoutedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetFrontSonarSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void BottomRightIRSensor_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateIRStatus();
            await LemurianHub.Invoke<bool>("SetBottomRightIRSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void BottomLeftIRSensor_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateIRStatus();
            await LemurianHub.Invoke<bool>("SetBottomLeftIRSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void TopRightIRSensor_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateIRStatus();
            await LemurianHub.Invoke<bool>("SetTopRightIRSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void TopLeftIRSensor_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateIRStatus();
            await LemurianHub.Invoke<bool>("SetTopLeftIRSensor", ((ToggleSwitch)sender).IsOn);
        }

        private async void StartMotorBSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetStartMotorBSpeed", (int)e.NewValue);
        }

        private async void StartMotorASpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetStartMotorASpeed", (int)e.NewValue);
        }

        private async void MaxMotorBSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetMaxMotorBSpeed", (int)e.NewValue);
        }

        private async void MaxMotorASpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            await LemurianHub.Invoke<bool>("SetMaxMotorASpeed", (int)e.NewValue);
        }
        #endregion
    }
}

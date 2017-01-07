using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.IoT.Lightning.Providers;
using Microsoft.ProjectOxford.Face;
using Sensors.Dht;
using Shasta.Facial_Recognition;
using Shasta.Helpers;
using Shasta.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
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
        private WebcamHelper webcam;
        private StorageFile currentIdPhotoFile;

        // Whitelist Related Variables:
        private StorageFolder whitelistFolder;
        private bool currentlyUpdatingWhitelist;

        // Oxford Related Variables:
        private bool initializedOxford = false;

        // create instance of a DHT11 
        private IDht _dhtInterface = null;

        public MainPage()
        {
            this.InitializeComponent();
            LemuriaConnect();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitGPIO();
            InitHelpers();
            InitOxford();
        }

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
                    StaticComponents.speedPin = null;
                    StaticComponents.directionForwardPin = null;
                    StaticComponents.directionBackwardPin = null;
                    StaticComponents.turnPin = null;
                    StaticComponents.turnLeftPin = null;
                    StaticComponents.turnRightPin = null;
                    GpioStatus.Text = "There is no GPIO controller on this device.";
                    return;
                }

                status = "GPIO is using Lightning provider and ";
                var pwmControllers = await PwmController.GetControllersAsync(LightningPwmProvider.GetPwmProvider());
                var pwmController = pwmControllers[1]; // Use the pwmController on raspberry pi
                pwmController.SetDesiredFrequency(50); // set frequency to 50Hz
                var gpioControllers = await GpioController.GetControllersAsync(LightningGpioProvider.GetGpioProvider());
                var gpioController = gpioControllers[0];

                // Enable the L293D drivers Motor Speed
                StaticComponents.speedPin = pwmController.OpenPin(StaticComponents.ENABLE_L293D_MOTOR_SPEED);
                StaticComponents.speedPin.SetActiveDutyCyclePercentage(0);
                StaticComponents.speedPin.Start();

                // Enable direction forward
                StaticComponents.directionForwardPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_FORWARD);
                StaticComponents.directionForwardPin.Write(GpioPinValue.Low);
                StaticComponents.directionForwardPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable direction forward
                StaticComponents.directionBackwardPin = gpioController.OpenPin(StaticComponents.MOTOR_DIRECTION_BACKWARD);
                StaticComponents.directionBackwardPin.Write(GpioPinValue.Low);
                StaticComponents.directionBackwardPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable the L293D drivers Motor Turn
                StaticComponents.turnPin = pwmController.OpenPin(StaticComponents.ENABLE_L293D_MOTOR_TURN);
                StaticComponents.turnPin.SetActiveDutyCyclePercentage(0);
                StaticComponents.turnPin.Start();

                // Enable turn left
                StaticComponents.turnLeftPin = gpioController.OpenPin(StaticComponents.MOTOR_TURN_LEFT);
                StaticComponents.turnLeftPin.Write(GpioPinValue.Low);
                StaticComponents.turnLeftPin.SetDriveMode(GpioPinDriveMode.Output);

                // Enable turn right
                StaticComponents.turnRightPin = gpioController.OpenPin(StaticComponents.MOTOR_TURN_RIGHT);
                StaticComponents.turnRightPin.Write(GpioPinValue.Low);
                StaticComponents.turnRightPin.SetDriveMode(GpioPinDriveMode.Output);

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

                // Initialize the Lemuria Hub
                LemuriaHubStat.Text = "Loading";
                InitHub();
            }
            else
            {
                GpioStatus.Text = "LightningProvider is not enabled";
            }

        }

        private async void InitHub()
        {
            try
            {
                var hubConnection = new HubConnection(StaticComponents.LocalWifiHub);
                var lemuriaHub = hubConnection.CreateHubProxy("LemuriaHub");
                lemuriaHub.On<bool>("OnConnectionChanged", connectCallback);
                lemuriaHub.On<Tuple<string, string, int, int>>("NavigateRobot", NavigateRobotCallback);
                await hubConnection.Start(new LongPollingTransport());

                // Start the Lemuria
                string connectApi = StaticComponents.LocalWifiHub + "api/lemuria?isConnect=true";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(connectApi);
                await request.GetResponseAsync();
            }
            catch (Exception)
            {
                StaticComponents.IsLemuriaHubConnected = false;
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

        private void LemuriaConnect()
        {
            enableInputForward.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputBackward.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputLeft.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputRight.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            motorSpeed.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            motorTurn.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            enableInputLeft.IsEnabled = StaticComponents.IsLemuriaHubConnected;
            SendSonar.IsEnabled = StaticComponents.IsLemuriaHubConnected;
        }

        // Call Backs

        private void connectCallback(bool connect)
        {
            StaticComponents.IsLemuriaHubConnected = connect;
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                // Your UI update code goes here!
                if (connect)
                    LemuriaHubStat.Text = "Connected";
                else LemuriaHubStat.Text = "Not Connected";

                LemuriaConnect();
            }).AsTask();
        }

        private void NavigateRobotCallback(Tuple<string, string, int, int> navigate)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                // Your UI update code goes here!
                string fbTag = navigate.Item1;
                string lrTag = navigate.Item2;
                int directionSpeed = navigate.Item3;
                int turnSpeed = navigate.Item4;

                if (fbTag == "front" & !enableInputForward.IsOn) enableInputForward.IsOn = true;
                else if (fbTag == "back" & !enableInputBackward.IsOn) enableInputBackward.IsOn = true;
                else
                {
                    enableInputForward.IsOn = false;
                    enableInputBackward.IsOn = false;
                }

                if (lrTag == "left" & !enableInputLeft.IsOn) enableInputLeft.IsOn = true;
                else if (lrTag == "right" & !enableInputRight.IsOn) enableInputRight.IsOn = true;
                else
                {
                    enableInputLeft.IsOn = false;
                    enableInputRight.IsOn = false;
                }

                motorSpeed.Value = directionSpeed;
                motorTurn.Value = turnSpeed;
            }).AsTask();
        }

        // UI Events

        private void DisableDirection()
        {
            enableInputForward.IsOn = false;
            enableInputBackward.IsOn = false;
            StaticMethods.enableInput(true, false, enableInputForward.IsOn);
            StaticMethods.enableInput(false, true, enableInputBackward.IsOn);
            ForwardIndicator.Foreground = StaticComponents.inactiveBrush;
            BackwardIndicator.Foreground = StaticComponents.inactiveBrush;
        }

        private void DisableTurn()
        {
            enableInputLeft.IsOn = false;
            enableInputRight.IsOn = false;
            StaticMethods.enableTurn(true, false, enableInputLeft.IsOn);
            StaticMethods.enableTurn(false, true, enableInputRight.IsOn);
            LeftIndicator.Foreground = StaticComponents.inactiveBrush;
            RightIndicator.Foreground = StaticComponents.inactiveBrush;
        }

        private void enableInputForward_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputForward.IsOn)
            {
                enableInputBackward.IsOn = false;
                StaticMethods.enableInput(true, false, enableInputForward.IsOn);
                ForwardIndicator.Foreground = StaticComponents.activeBrush;
                BackwardIndicator.Foreground = StaticComponents.inactiveBrush;
            }
            else DisableDirection();
        }

        private void enableInputBackward_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputBackward.IsOn)
            {
                enableInputForward.IsOn = false;
                StaticMethods.enableInput(false, true, enableInputBackward.IsOn);
                BackwardIndicator.Foreground = StaticComponents.activeBrush;
                ForwardIndicator.Foreground = StaticComponents.inactiveBrush;
            }
            else DisableDirection();
        }

        private void motorSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StaticComponents.speedPin.SetActiveDutyCyclePercentage(e.NewValue * .01);
        }

        private void enableInputLeft_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputLeft.IsOn)
            {
                enableInputRight.IsOn = false;
                StaticMethods.enableTurn(true, false, enableInputLeft.IsOn);
                LeftIndicator.Foreground = StaticComponents.activeBrush;
                RightIndicator.Foreground = StaticComponents.inactiveBrush;
            }
            else DisableTurn();
        }

        private void enableInputRight_Toggled(object sender, RoutedEventArgs e)
        {
            if (enableInputRight.IsOn)
            {
                enableInputLeft.IsOn = false;
                StaticMethods.enableTurn(true, false, enableInputRight.IsOn);
                RightIndicator.Foreground = StaticComponents.activeBrush;
                LeftIndicator.Foreground = StaticComponents.inactiveBrush;
            }
            else DisableTurn();
        }

        private void motorTurn_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            StaticComponents.turnPin.SetActiveDutyCyclePercentage(e.NewValue * .01);
        }

        private void SendSonar_Toggled(object sender, RoutedEventArgs e)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                while (SendSonar.IsOn)
                {
                    // Your UI update code goes here!
                    await StaticMethods.SendPulse();
                    var distance = StaticMethods.ReceivePulse();

                    if (distance > 2 && distance < 30)         //Check whether the distance is within range
                        SonarLog.Text = distance + " cm";      //Print distance with 0.5 cm calibration
                    else
                        SonarLog.Text = "Out Of Range";        //display out of range

                    await Task.Delay(100);
                }

                if (!SendSonar.IsOn)
                {
                    SonarLog.Text = "Distance";
                }
            }).AsTask();
        }


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
            WhiteListedViewModels.RemoveAllItems();

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

        private Task<bool> HasInfra1Obstacle()
        {
            bool hasObject = false;

            if (StaticComponents.infra1Pin.Read() == GpioPinValue.High)
                hasObject = true;

            return Task.FromResult<bool>(hasObject);
        }

        private Task<bool> HasInfra2Obstacle()
        {
            bool hasObject = false;

            if (StaticComponents.infra2Pin.Read() == GpioPinValue.High)
                hasObject = true;

            return Task.FromResult<bool>(hasObject);
        }

        private Task<bool> HasInfra3Obstacle()
        {
            bool hasObject = false;

            if (StaticComponents.infra2Pin.Read() == GpioPinValue.High)
                hasObject = true;

            return Task.FromResult<bool>(hasObject);
        }

        private Task<bool> HasInfra4Obstacle()
        {
            bool hasObject = false;

            if (StaticComponents.infra4Pin.Read() == GpioPinValue.High)
                hasObject = true;

            return Task.FromResult<bool>(hasObject);
        }

        private async void Infra1Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra1Detect.IsOn)
            {
                if (await HasInfra1Obstacle())
                {
                    Infra1Detected.Text = "Obstacle Detected";
                }
                else Infra1Detected.Text = "No Obstacle";

                await Task.Delay(1000);
            }
        }

        private async void Infra2Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra2Detect.IsOn)
            {
                if (await HasInfra2Obstacle())
                {
                    Infra2Detected.Text = "Obstacle Detected";
                }
                else Infra2Detected.Text = "No Obstacle";

                await Task.Delay(1000);
            }
        }

        private async void Infra3Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra1Detect.IsOn)
            {
                if (await HasInfra3Obstacle())
                {
                    Infra3Detected.Text = "Obstacle Detected";
                }
                else Infra3Detected.Text = "No Obstacle";

                await Task.Delay(1000);
            }
        }

        private async void Infra4Detect_Toggled(object sender, RoutedEventArgs e)
        {
            while (Infra1Detect.IsOn)
            {
                if (await HasInfra4Obstacle())
                {
                    Infra4Detected.Text = "Obstacle Detected";
                }
                else Infra4Detected.Text = "No Obstacle";

                await Task.Delay(1000);
            }
        }

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
                var tempHum = await GetTemperatureAndHumidity();
                TemperatureText.Text = tempHum.Item1.ToString() + " °C";
                HumidityText.Text = tempHum.Item2.ToString() + " RH";
                await Task.Delay(1500);
            }
        }
    }
}

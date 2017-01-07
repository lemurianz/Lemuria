using ShastaController.uwp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
        public MainPage()
        {
            this.InitializeComponent();
            LemuriaPivot.Visibility = Visibility.Collapsed;
            ledStripLights = new LEDStripLight();

            ApplicationView.PreferredLaunchViewSize = new Size(700, 450);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private async void MoveForward_Click(object sender, RoutedEventArgs e)
        {
            IRTopLeftTextblock.Text = "Connecting";
            await ledStripLights.ConnectAsync("192.168.0.59", "5000");
            IRTopLeftTextblock.Text = "Connected";
        }

        private void MoveBackward_Click(object sender, RoutedEventArgs e)
        {
            ledStripLights.WriteToLight(30, 12, 78, 60);
        }

        private void TurnLeft_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TurnRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void LemuriaPivot_Loaded(object sender, RoutedEventArgs e)
        {
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
                LemuriaLoading.Visibility = Visibility.Collapsed;
                LemuriaPivot.Visibility = Visibility.Visible;
            }
        }
    }
}

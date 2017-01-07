using Shasta.Facial_Recognition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Shasta.ViewModels
{
    class WhiteListedViewModels
    {
        public static ObservableCollection<Visitor> WhitelistedVisitors { get; set; }

        private void AddItems(int numberOfFakes)
        {
            var pictureCounter = 1;
            for (int i = 0; i < numberOfFakes; i++)
            {
                // Reset picture counter
                if (pictureCounter > 9)
                    pictureCounter = 1;

                string[] unisexNames = new string[] { "Addison", "Adrian", "Alex", "Alexis", "Angel", "Aubrey", "Whitney", "Jerry", "Jesse" };
                WhitelistedVisitors.Add(new Visitor
                (
                    name: unisexNames[pictureCounter -1],
                    imageFolder: null,
                    image: new BitmapImage(new Uri("ms-appx://Shasta/Assets/" + pictureCounter.ToString() + ".png"))
                ));

                pictureCounter++;
            }
        }

        public WhiteListedViewModels()
        {
            WhitelistedVisitors = new ObservableCollection<Visitor>();
            AddItems(5);
        }

        public static void RemoveAllItems()
        {
            WhitelistedVisitors.Clear();
        }
    }
}

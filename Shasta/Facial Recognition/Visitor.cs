using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Shasta.Facial_Recognition
{
    /// <summary>
    /// Contains information such as name and location of ID images in storage for each authorized visitor to the Facial Recognition Door
    /// </summary>
    public class Visitor : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Name of the person
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The image folder
        /// </summary>
        public StorageFolder ImageFolder { get; set; }

        /// <summary>
        /// The person's image
        /// </summary>
        private BitmapImage image;
        public BitmapImage Image
        {
            get { return image; }
            set
            {
                image = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("Image");
            }
        }

        /// <summary>
        /// Initializes a new visitor object with relevant information
        /// </summary>
        public Visitor(string name, StorageFolder imageFolder, BitmapImage image)
        {
            Name = name;
            ImageFolder = imageFolder;
            Image = image;
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}

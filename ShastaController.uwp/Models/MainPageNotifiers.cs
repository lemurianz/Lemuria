using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShastaController.uwp.Models
{
    public class MainPageNotifiers : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isForwardPressed;
        public bool IsForwardPressed
        {
            get { return isForwardPressed; }
            set
            {
                isForwardPressed = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("IsForwardPressed");
            }
        }

        private bool isBackwardPressed;
        public bool IsBackwardPressed
        {
            get { return isBackwardPressed; }
            set
            {
                isBackwardPressed = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("IsBackwardPressed");
            }
        }

        private bool isLeftPressed;
        public bool IsLeftPressed
        {
            get { return isLeftPressed; }
            set
            {
                isLeftPressed = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("IsLeftPressed");
            }
        }

        private bool isRightPressed;
        public bool IsRightPressed
        {
            get { return isRightPressed; }
            set
            {
                isRightPressed = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("IsRightPressed");
            }
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

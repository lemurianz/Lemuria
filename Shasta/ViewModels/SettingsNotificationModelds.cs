using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shasta.ViewModels
{
    public class SettingsNotificationModelds : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;


        private int maxMotorSpeedA;
        public int MaxMotorSpeedA
        {
            get { return maxMotorSpeedA; }
            set
            {
                maxMotorSpeedA = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("MaxMotorSpeedA");
            }
        }

        private int maxMotorSpeedB;
        public int MaxMotorSpeedB
        {
            get { return maxMotorSpeedB; }
            set
            {
                maxMotorSpeedB = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("MaxMotorSpeedB");
            }
        }

        private bool topLeftIR;
        public bool TopLeftIR
        {
            get { return topLeftIR; }
            set
            {
                topLeftIR = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("TopLeftIR");
            }
        }

        private bool topRightIR;
        public bool TopRightIR
        {
            get { return topRightIR; }
            set
            {
                topRightIR = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("TopRightIR");
            }
        }

        private bool bottomLeftIR;
        public bool BottomLeftIR
        {
            get { return bottomLeftIR; }
            set
            {
                bottomLeftIR = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("BottomLeftIR");
            }
        }

        private bool bottomRightIR;
        public bool BottomRightIR
        {
            get { return bottomRightIR; }
            set
            {
                bottomRightIR = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("BottomRightIR");
            }
        }


        private bool frontSonar;
        public bool FrontSonar
        {
            get { return frontSonar; }
            set
            {
                frontSonar = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("FrontSonar");
            }
        }

        private bool backSonar;
        public bool BackSonar
        {
            get { return backSonar; }
            set
            {
                backSonar = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("BackSonar");
            }
        }

        private bool internalTempAndHum;
        public bool InternalTempAndHum
        {
            get { return internalTempAndHum; }
            set
            {
                internalTempAndHum = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("InternalTempAndHum");
            }
        }

        private bool faceDetect;
        public bool FaceDetect
        {
            get { return faceDetect; }
            set
            {
                faceDetect = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("FaceDetect");
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

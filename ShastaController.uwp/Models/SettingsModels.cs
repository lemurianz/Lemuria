using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShastaController.uwp.Models
{
    public class SettingsModels
    {
        public int MaxMotorSpeedA { get; set; }
        public int MaxMotorSpeedB { get; set; }
        public bool TopLeftIR { get; set; }
        public bool TopRightIR { get; set; }
        public bool BottomLeftIR { get; set; }
        public bool BottomRightIR { get; set; }
        public bool FrontSonar { get; set; }
        public bool BackSonar { get; set; }
        public bool InternalTempAndHum { get; set; }
        public bool FaceDetect { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shasta.Models
{
    public class InfraSonarUpdateModels
    {
        public bool InfraTopLeft { get; set; }
        public bool InfraTopRight { get; set; }
        public bool InfraBottomLeft { get; set; }
        public bool InfraBottomRight { get; set; }
        public double FrontSonarDistance { get; set; }
    }
}

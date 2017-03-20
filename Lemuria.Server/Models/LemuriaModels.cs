using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lemuria.Server.Models
{
    public class LemuriaModels
    {
        public bool SetConnected { get; set; }
        public string FBTag { get; set; }
        public string LRTag { get; set; }
        public int DirectionSpeed { get; set; }
        public int TurnSpeed { get; set; }
        public int MyProperty { get; set; }
    }
}
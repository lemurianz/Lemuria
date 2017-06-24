﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lemuria.Server.Models
{
    public class MessageReplyModels
    {
        public string ReceivedMessage { get; set; }
        public string ReplyMessage { get; set; }
        public Dictionary<string, List<string>> Methods { get; set; }
    }
}
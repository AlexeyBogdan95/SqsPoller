using System;
using System.Collections.Generic;

namespace SqsPoller
{
    internal class MessageBody
    {
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, MessageAttribute> MessageAttributes { get; set; }
            = new Dictionary<string, MessageAttribute>();
        public DateTime TimeStamp { get; set; }
    }
    
    internal class MessageAttribute
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
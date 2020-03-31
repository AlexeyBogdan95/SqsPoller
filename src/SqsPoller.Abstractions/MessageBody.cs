using System;
using System.Collections.Generic;

namespace SqsPoller.Abstractions
{
    public class MessageBody
    {
        public string Message { get; set; }
        public Dictionary<string, MessageAttribute> MessageAttributes { get; set; } = new Dictionary<string, MessageAttribute>();
        public DateTime TimeStamp { get; set; }
    }
    public class MessageAttribute
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
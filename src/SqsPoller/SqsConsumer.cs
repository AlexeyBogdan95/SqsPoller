using System;

namespace SqsPoller
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqsConsumer : Attribute
    {
        public string MessageAttribute { get; set; }
        public string Value { get; set; }
    }
}
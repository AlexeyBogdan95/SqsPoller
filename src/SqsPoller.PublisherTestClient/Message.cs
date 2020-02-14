using System.Collections.Generic;

namespace SqsPoller.PublisherTestClient
{
    public class Message
    {
        public string Body { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
    }
}
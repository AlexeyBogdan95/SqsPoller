using System.Collections.Generic;

namespace SqsPoller.PublisherTestClient
{
    public class FirstTestMessage
    {
        public string FirstProperty { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
    }
}
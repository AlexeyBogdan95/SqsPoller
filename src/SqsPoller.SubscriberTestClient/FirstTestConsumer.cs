using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.PublisherTestClient;

namespace SqsPoller.SubscriberTestClient
{
    public class FirstTestConsumer : IConsumer<FirstTestMessage>
    {
        public Task Consume(FirstTestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Message consumed: {0}", JsonConvert.SerializeObject(message));
            return Task.CompletedTask;
        }
    }
}
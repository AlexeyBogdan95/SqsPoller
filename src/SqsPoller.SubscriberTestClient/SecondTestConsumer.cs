using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.PublisherTestClient;

namespace SqsPoller.SubscriberTestClient
{
    public class SecondTestConsumer : IConsumer<SecondTestMessage>
    {
        public Task Consume(SecondTestMessage message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Second message consumed: {0}", JsonConvert.SerializeObject(message));
            return Task.CompletedTask;
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqsPoller.Sample.Subscriber
{
    internal class FooConsumer : IConsumer<FooMessage>
    {
        private readonly ILogger<FooConsumer> _logger;

        public FooConsumer(ILogger<FooConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(FooMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("FooMessage.Value = {value}", message.Value);
            return Task.CompletedTask;
        }
    }
}
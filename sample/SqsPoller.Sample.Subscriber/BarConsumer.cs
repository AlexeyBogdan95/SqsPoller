using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqsPoller.Sample.Subscriber
{
    internal class BarConsumer : IConsumer<BarMessage>
    {
        private readonly ILogger<BarConsumer> _logger;

        public BarConsumer(ILogger<BarConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(BarMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("BarMessage.Value = {value}", message.Value);
            return Task.CompletedTask;
        }
    }
}
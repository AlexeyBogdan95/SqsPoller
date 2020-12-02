using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqsPoller.Sample.Subscriber
{
    [SqsConsumer(MessageAttribute = "event", Value = "custom_route_message")]
    internal class CustomRouteMessageConsumer : IConsumer<CustomRouteMessage>
    {
        private readonly ILogger<CustomRouteMessageConsumer> _logger;

        public CustomRouteMessageConsumer(ILogger<CustomRouteMessageConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(CustomRouteMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CustomRouteMessage.Value = {value}", message.Value);
            return Task.CompletedTask;
        }
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqsPooler
{
    internal interface IConsumerResolver
    {
        Task Resolve(string message, string messageType, CancellationToken cancellationToken);
    }
    
    internal class ConsumerResolver: IConsumerResolver
    {
        private readonly IEnumerable<IConsumer> _consumers;

        public ConsumerResolver(IEnumerable<IConsumer> consumers)
        {
            _consumers = consumers;
        }

        public Task Resolve(string message, string messageType, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
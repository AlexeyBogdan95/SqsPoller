using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace SqsPoller
{
    internal class ConsumerResolver: IConsumerResolver
    {
        private readonly IEnumerable<IConsumer> _consumers;

        public ConsumerResolver(IEnumerable<IConsumer> consumers)
        {
            _consumers = consumers;
        }

        public void Resolve(string message, string messageType, CancellationToken cancellationToken)
        {
            foreach (var consumer in _consumers)
            {
                var consumerType = consumer.GetType().GetInterfaces()
                    .Where(type => type.IsGenericType)
                    .Where(type => type.GetGenericTypeDefinition() == typeof(IConsumer<>))
                    .Select(type => type.GetGenericArguments().Single())
                    .FirstOrDefault(type => type.Name == messageType);
                
                if (consumerType == null)
                    continue;
                
                var deserializedMessage = JsonConvert.DeserializeObject(message, consumerType);
                var @params = new[]
                {
                    deserializedMessage,
                    cancellationToken
                };
                consumer.GetType().GetMethod("Consume")?.Invoke(consumer, @params);
                return;
            }

            throw new ConsumerNotFoundException(messageType);
        }
    }
}
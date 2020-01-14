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
                var type = consumer.GetType().GetInterfaces()
                    .Where(x => x.IsGenericType)
                    .Where(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>))
                    .Select(x => x.GetGenericArguments().Single())
                    .FirstOrDefault(x => x.Name == messageType);
                
                if (type == null)
                    continue;
                
                var deserializedMessage = JsonConvert.DeserializeObject(message, type);
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
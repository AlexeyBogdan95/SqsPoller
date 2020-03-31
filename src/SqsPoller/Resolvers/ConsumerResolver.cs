using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqsPoller.Abstractions;

namespace SqsPoller.Resolvers
{
    internal class ConsumerResolver : IConsumerResolver
    {
        private readonly IEnumerable<IConsumer> _consumers;

        public ConsumerResolver(IEnumerable<IConsumer> consumers)
        {
            _consumers = consumers;
        }

        public ConsumerResolver(IEnumerable<IConsumer> consumers, IEnumerable<Type> consumerTypes)
        {
            _consumers = consumers.Where(c => consumerTypes.Contains(c.GetType()));
        }

        public async Task Resolve(string message, string messageType, CancellationToken cancellationToken = default)
        {
            foreach (var consumer in _consumers)
            {
                var consumerType = consumer.GetType().GetInterfaces()
                    .Where(type => type.IsGenericType)
                    .Where(type => type.GetGenericTypeDefinition() == typeof(IConsumer<>))
                    .Select(type => type.GetGenericArguments().Single())
                    .FirstOrDefault(type => type.FullName == messageType);
                
                if (consumerType == null)
                    continue;
                
                var deserializedMessage = JsonConvert.DeserializeObject(message, consumerType);
                var @params = new[]
                {
                    deserializedMessage,
                    cancellationToken
                };
                
                await (Task) consumer.GetType().GetMethod("Consume")?.Invoke(consumer, @params);
                return;
            }

            throw new ConsumerNotFoundException(messageType);
        }
    }
}
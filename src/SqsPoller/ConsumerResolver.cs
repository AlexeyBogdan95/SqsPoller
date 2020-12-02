using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SqsPoller
{

    public class ConsumerResolver: IConsumerResolver
    {
        private readonly IEnumerable<(IConsumer instance, Type messageType, SqsConsumer mapping)> _consumersMapping;
        private readonly ILogger<ConsumerResolver> _logger;

        public ConsumerResolver(IEnumerable<IConsumer> consumers,
            ILogger<ConsumerResolver> logger)
        {
            _logger = logger;
            _consumersMapping = consumers.Select(c =>
                {
                    var type = c.GetType();

                    var messageType = type.GetInterfaces()
                        .Where(t => t.IsGenericType)
                        .SingleOrDefault(t => t.GetGenericTypeDefinition() == typeof(IConsumer<>))
                        ?.GetGenericArguments().Single();

                    if (messageType == null)
                    {
                        throw new ArgumentException("Please specify message type in generic argument of IConsumer<>");
                    }

                    var consumer =
                        type.GetCustomAttributes(typeof(SqsConsumer), false).FirstOrDefault() as SqsConsumer;

                    if (consumer != null)
                    {
                        return (c, messageType, consumer);
                    }

                    return (c, messageType, new SqsConsumer
                    {
                        Value = messageType.Name,
                        MessageAttribute = "MessageType"
                    });
                });
        }

        public async Task Resolve(Message message, CancellationToken cancellationToken)
        {
            foreach (var consumerMapping in _consumersMapping)
            {
                var messageType = message.MessageAttributes
                    .FirstOrDefault(pair => pair.Key == consumerMapping.mapping.MessageAttribute)
                    .Value?.StringValue;

                string messageBody;
                if (messageType != null && messageType == consumerMapping.mapping.Value)
                {
                    _logger.LogTrace("Message Type is {message_type}", messageType);
                    messageBody = message.Body;
                }
                else
                {
                    var body = JsonConvert.DeserializeObject<MessageBody>(message.Body);
                    messageType = body.MessageAttributes
                        .FirstOrDefault(pair => pair.Key == consumerMapping.mapping.MessageAttribute).Value?.Value;

                    if(messageType == null || messageType != consumerMapping.mapping.Value)
                        continue;

                    _logger.LogTrace("Message Type is {message_type}", messageType);
                    messageBody = body.Message;
                }

                var deserializedMessage = JsonConvert.DeserializeObject(messageBody, consumerMapping.messageType);
                var @params = new[]
                {
                    deserializedMessage,
                    cancellationToken
                };

                await (Task) consumerMapping.instance.GetType().GetMethod("Consume")
                    .Invoke(consumerMapping.instance, @params);
                return;
            }

            throw new ConsumerNotFoundException(message.MessageId);
        }
    }
}
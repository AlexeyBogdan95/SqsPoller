using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            _consumersMapping = consumers.SelectMany(c =>
                {
                    var type = c.GetType();

                    var messageTypes = type.GetInterfaces()
                        .Where(t => t.IsGenericType)
                        .Where(t => t.GetGenericTypeDefinition() == typeof(IConsumer<>))
                        .Select(t => t.GetGenericArguments().Single())
                        .ToArray();

                    if (messageTypes.Length == 0)
                    {
                        throw new ArgumentException("Please specify message type in generic argument of IConsumer<>");
                    }

                    var consumer =
                        type.GetCustomAttributes(typeof(SqsConsumer), false).FirstOrDefault() as SqsConsumer;

                    if (consumer != null)
                    {
                        return messageTypes.Select(t => (c, t, consumer));
                    }

                    return messageTypes.Select(t => (c, t, new SqsConsumer
                    {
                        Value = t.Name,
                        MessageAttribute = "MessageType"
                    }));
                });
        }

        public async Task Resolve(Message message, CancellationToken cancellationToken)
        {
            bool consumerNotFound = true;
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

                await (Task) consumerMapping.instance.GetType().GetMethod(
                        "Consume",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        CallingConventions.Any,
                        new [] {consumerMapping.messageType, typeof(CancellationToken)},
                        null)
                    .Invoke(consumerMapping.instance, @params);
                consumerNotFound = false;
            }

            if (consumerNotFound)
            { 
                throw new ConsumerNotFoundException(message.MessageId);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SqsPoller
{

    public class ConsumerResolver: IConsumerResolver
    {
        private readonly IEnumerable<(Type consumerType, Type messageType, SqsConsumer mapping)> _consumersMapping;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsumerResolver> _logger;
        private readonly JsonSerializerOptions _options;

        public ConsumerResolver(IServiceProvider serviceProvider, IEnumerable<Type> consumers,
            ILogger<ConsumerResolver> logger, JsonConverter? jsonConverter = default)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _consumersMapping = consumers.SelectMany(type =>
                {
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
                        return messageTypes.Select(t => (type, t, consumer));
                    }

                    return messageTypes.Select(t => (type, t, new SqsConsumer
                    {
                        Value = t.Name,
                        MessageAttribute = "MessageType"
                    }));
                });

            _options = jsonConverter == default
                ? new JsonSerializerOptions { PropertyNameCaseInsensitive = true, }
                : new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { jsonConverter } };
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
                    var body = JsonSerializer.Deserialize<MessageBody>(message.Body, _options);
                    messageType = body.MessageAttributes
                        .FirstOrDefault(pair => pair.Key == consumerMapping.mapping.MessageAttribute).Value?.Value;

                    if(messageType == null || messageType != consumerMapping.mapping.Value)
                        continue;

                    _logger.LogTrace("Message Type is {message_type}", messageType);
                    messageBody = body.Message;
                }

                var deserializedMessage = JsonSerializer.Deserialize(messageBody, consumerMapping.messageType, _options);
                var @params = new[]
                {
                    deserializedMessage,
                    cancellationToken
                };

                var instance = _serviceProvider.GetRequiredService(consumerMapping.consumerType);
                await (Task) instance.GetType().GetMethod(
                        "Consume",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        CallingConventions.Any,
                        [consumerMapping.messageType, typeof(CancellationToken)],
                        null)!
                    .Invoke(instance, @params);
                consumerNotFound = false;
            }

            if (consumerNotFound)
            { 
                throw new ConsumerNotFoundException(message.MessageId);
            }
        }
    }
}
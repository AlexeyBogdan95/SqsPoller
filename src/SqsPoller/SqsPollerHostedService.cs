using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.Resolvers;

namespace SqsPoller
{
    internal class SqsPollerHostedService : BackgroundService
    {
        private readonly AmazonSqsReciever _amazonSqsReciever;
        private readonly IConsumerResolver _consumerResolver;
        private readonly ILogger<SqsPollerHostedService> _logger;

        public SqsPollerHostedService(
            AmazonSqsReciever amazonSqsReciever,
            IConsumerResolver consumerResolver,
            ILogger<SqsPollerHostedService> logger)
        {
            _amazonSqsReciever = amazonSqsReciever;
            _consumerResolver = consumerResolver;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Handle(stoppingToken);
            }
        }

        private async Task Handle(CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["correlation_id"] = Guid.NewGuid()
            }))
            {
                _logger.LogDebug("Start polling messages from a queue. correlation_id: {correlation_id}");
                ReceiveMessageResponse receiveMessageResult;
                try
                {
                    receiveMessageResult = await _amazonSqsReciever.ReceiveMessageAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to receive messages from the queue");
                    return;
                }

                var messagesCount = receiveMessageResult.Messages.Count;
                _logger.LogDebug("{count} messages received", messagesCount);

                foreach (var msg in receiveMessageResult.Messages)
                {
                    try
                    {
                        var messageType = msg.MessageAttributes
                            .FirstOrDefault(x => x.Key == Constants.MessageType).Value?.StringValue;

                        using (_logger.BeginScope(new Dictionary<string, object>
                        {
                            ["message_type"] = messageType
                        }))
                        {
                            if (messageType != null)
                            {
                                _logger.LogDebug("Message Type is {message_type}");
                                _consumerResolver.Resolve(msg.Body, messageType, cancellationToken);
                            }
                            else
                            {
                                var body = JsonConvert.DeserializeObject<MessageBody>(msg.Body);
                                messageType = body.MessageAttributes.FirstOrDefault(x => x.Key == Constants.MessageType).Value.Value;
                                _logger.LogDebug("Message Type is {message_type}");
                                _consumerResolver.Resolve(body.Message, messageType, cancellationToken);
                            }
                        }

                        _logger.LogDebug("Deleting the message {message_id}", msg.ReceiptHandle);
                        await _amazonSqsReciever.DeleteMessageAsync(msg.ReceiptHandle, cancellationToken);

                        _logger.LogDebug(
                            "The message {message_id} has been deleted successfully",
                            msg.ReceiptHandle);

                        if (cancellationToken.IsCancellationRequested)
                            return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "Failed to handle message {message_id}. {@ex}", msg.ReceiptHandle, ex);
                    }
                }
            }
        }
    }
}
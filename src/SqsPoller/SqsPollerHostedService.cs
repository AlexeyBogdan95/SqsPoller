using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SqsPoller
{
    internal class SqsPoolerHostedService: BackgroundService
    {
        private readonly AmazonSQSClient _amazonSqsClient;
        private readonly SqsPoolerConfig _config;
        private readonly IConsumerResolver _consumerResolver;
        private readonly ILogger<SqsPoolerHostedService> _logger;

        public SqsPoolerHostedService(
            AmazonSQSClient amazonSqsClient, 
            SqsPoolerConfig config, 
            IConsumerResolver consumerResolver,
            ILogger<SqsPoolerHostedService> logger)
        {
            _amazonSqsClient = amazonSqsClient;
            _config = config;
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
                _logger.LogDebug("Start pooling messages from a queue. correlation_id: {correlation_id}");
                ReceiveMessageResponse receiveMessageResult = null;
                try
                {
                    receiveMessageResult = await _amazonSqsClient
                        .ReceiveMessageAsync(new ReceiveMessageRequest
                        {
                            WaitTimeSeconds = _config.WaitTimeSeconds,
                            MaxNumberOfMessages = _config.MaxNumberOfMessages,
                            MessageAttributeNames = _config.MessageAttributeNames,
                            QueueUrl = _config.QueueUrl
                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to receive messages from the queue");
                }


                var messagesCount = receiveMessageResult.Messages.Count;
                _logger.LogDebug("{count} messages received", messagesCount);

                foreach (var msg in receiveMessageResult.Messages)
                {
                    try
                    {
                        var messageType = msg.MessageAttributes
                            .FirstOrDefault(x => x.Key == "MessageType").Value?.StringValue;

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
                                messageType = body.MessageAttributes
                                    .FirstOrDefault(x => x.Key == "MessageType").Value.Value;
                                _logger.LogDebug("Message Type is {message_type}");
                                _consumerResolver.Resolve(body.Message, messageType, cancellationToken);
                            }
                        }

                        _logger.LogDebug("Deleting the message {message_id}", msg.ReceiptHandle);
                        await _amazonSqsClient.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl = _config.QueueUrl,
                            ReceiptHandle = msg.ReceiptHandle
                        }, cancellationToken);

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
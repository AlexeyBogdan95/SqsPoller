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
    internal class SqsPollerHostedService: BackgroundService
    {
        private readonly AmazonSQSClient _amazonSqsClient;
        private readonly SqsPollerConfig _config;
        private readonly IConsumerResolver _consumerResolver;
        private readonly ILogger<SqsPollerHostedService> _logger;

        public SqsPollerHostedService(
            AmazonSQSClient amazonSqsClient, 
            SqsPollerConfig config, 
            IConsumerResolver consumerResolver,
            ILogger<SqsPollerHostedService> logger)
        {
            _amazonSqsClient = amazonSqsClient;
            _config = config;
            _consumerResolver = consumerResolver;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueUrl = !string.IsNullOrEmpty(_config.QueueUrl)
                ? _config.QueueUrl
                : (await _amazonSqsClient.GetQueueUrlAsync(_config.QueueName, stoppingToken)).QueueUrl;
            while (!stoppingToken.IsCancellationRequested)
            {
                await Handle(queueUrl, stoppingToken);
            }
        }

        private async Task Handle(string queueUrl, CancellationToken cancellationToken)
        {
            using var correlationIdScope = _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["correlation_id"] = Guid.NewGuid(),
                    ["queue_url"] = queueUrl
                });
            _logger.LogTrace("Start polling messages from a queue: {queue_url}. correlation_id: {correlation_id}");

            ReceiveMessageResponse receiveMessageResult = new ReceiveMessageResponse();
            try
            {
                receiveMessageResult = await _amazonSqsClient
                    .ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        WaitTimeSeconds = _config.WaitTimeSeconds,
                        MaxNumberOfMessages = _config.MaxNumberOfMessages,
                        MessageAttributeNames = _config.MessageAttributeNames,
                        QueueUrl = queueUrl
                    }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive messages from the queue");
            }
                
            var messagesCount = receiveMessageResult.Messages.Count;
            _logger.LogTrace("{count} messages received", messagesCount);
            foreach (var message in receiveMessageResult.Messages)
            {
                using var messageIdScope = _logger.BeginScope(
                    new Dictionary<string, object>
                    {
                        ["message_id"] = message.MessageId,
                        ["receipt_handle"] = message.ReceiptHandle
                    });
                _logger.LogTrace("Start processing the message with id {message_id} and ReceiptHandle {receipt_handle}");

                var messageType = message.MessageAttributes
                    .FirstOrDefault(pair => pair.Key == "MessageType")
                    .Value?.StringValue;
                
                string messageBody;
                if (messageType != null)
                {
                    _logger.LogTrace("Message Type is {message_type}", messageType);
                    messageBody = message.Body;
                }
                else
                {
                    var body = JsonConvert.DeserializeObject<MessageBody>(message.Body);
                    messageType = body.MessageAttributes
                        .FirstOrDefault(pair => pair.Key == "MessageType").Value.Value;
                    _logger.LogTrace("Message Type is {message_type}", messageType);
                    messageBody = body.Message;
                }
                    
                _consumerResolver
                    .Resolve(messageBody, messageType, cancellationToken)
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            _logger.LogError(task.Exception, "Failed to handle message with id {message_id} and ReceiptHandle {receipt_handle}");
                            return;
                        }
    
                        _logger.LogTrace("Deleting the message with id {message_id} and ReceiptHandle {receipt_handle}");
                        _amazonSqsClient
                            .DeleteMessageAsync(new DeleteMessageRequest
                            {
                                QueueUrl = queueUrl,
                                ReceiptHandle = message.ReceiptHandle
                            }, cancellationToken)
                            .ContinueWith(deleteMessageTask =>
                            {
                                if (deleteMessageTask.IsFaulted)
                                {
                                    _logger.LogError(task.Exception, "Failed to deleete message with id {message_id} and ReceiptHandle {receipt_handle}");
                                    return;
                                }
                                
                                _logger.LogTrace("The message with id {message_id} and ReceiptHandle {receipt_handle} has been deleted successfully");
                            }, cancellationToken);
                    }, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    return;
            }
        }
    }
}
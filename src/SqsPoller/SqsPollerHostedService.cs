using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SqsPoller
{
    public class SqsPollerHostedService: BackgroundService
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly SqsPollerConfig _config;
        private readonly IConsumerResolver _consumerResolver;
        private readonly ILogger<SqsPollerHostedService> _logger;

        public SqsPollerHostedService(
            IAmazonSQS amazonSqsClient,
            SqsPollerConfig config,
            IConsumerResolver consumerResolver,
            ILogger<SqsPollerHostedService> logger)
        {
            _amazonSqsClient = amazonSqsClient;
            _consumerResolver = consumerResolver;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueUrl = !string.IsNullOrEmpty(_config.QueueUrl)
                ? _config.QueueUrl
                : (await _amazonSqsClient.GetQueueUrlAsync(_config.QueueName, stoppingToken)).QueueUrl;
            using var semaphore = new SemaphoreSlim(_config.MaxNumberOfParallelism);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Handle(queueUrl, semaphore, stoppingToken);
            }
        }

        private async Task Handle(string queueUrl, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            using var correlationIdScope = _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["correlation_id"] = Guid.NewGuid(),
                    ["queue_url"] = queueUrl
                });

            _logger.LogTrace("Start polling messages from a queue: {queue_url}. correlation_id: {correlation_id}");
            var messages = await GetMessages(queueUrl, cancellationToken);
            foreach (var message in messages)
            {
                using var messageIdScope = _logger.BeginScope(
                    new Dictionary<string, object>
                    {
                        ["message_id"] = message.MessageId,
                        ["receipt_handle"] = message.ReceiptHandle
                    });
                _logger.LogTrace(
                    "Start processing the message with id {message_id} and ReceiptHandle {receipt_handle}");

                await semaphore.WaitAsync(cancellationToken);
                HandleMessage(message, cancellationToken, semaphore, queueUrl);
                if (cancellationToken.IsCancellationRequested)
                    return;
            }
        }

        public async Task HandleMessage(Message message, CancellationToken cancellationToken, SemaphoreSlim semaphore, string queueUrl)
        {
            try
            {
                _logger.LogTrace("Handling message {message_id} {receipt_handle}");
                await _consumerResolver.Resolve(message, cancellationToken);
                _logger.LogTrace("Message {message_id} {receipt_handle} has been handled");

                _logger.LogTrace("Deleting the message with id {message_id} and ReceiptHandle {receipt_handle}");
                DeleteMessage(queueUrl, message, cancellationToken);
                _logger.LogTrace("Message {message_id} {receipt_handle} has been deleted");
            }
            catch (Exception e)
            {
                if (_config.OnHandleMessageException is null)
                {
                    _logger.Log(_config.ExceptionDefaultMessageLogLevel, e, "Failed to handle message {message_id} {receipt_handle}");
                }
                else
                {
                    _config.OnHandleMessageException(e, message.MessageId);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task DeleteMessage(string queueUrl, Message message, CancellationToken cancellationToken)
        {
            try
            {
                var deleteRequest = new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                await _amazonSqsClient.DeleteMessageAsync(deleteRequest, cancellationToken);
            }
            catch (Exception e)
            {
                if (_config.OnDeleteMessageException is null)
                {
                    _logger.Log(
                        _config.ExceptionDefaultMessageLogLevel,
                        e,
                        "Failed to delete message {message_id} {receipt_handle}");
                }
                else
                {
                    _config.OnDeleteMessageException(e, message.MessageId);
                }
            }
        }

        private async Task<IEnumerable<Message>> GetMessages(
            string queueUrl, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _amazonSqsClient
                    .ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        WaitTimeSeconds = _config.WaitTimeSeconds,
                        MaxNumberOfMessages = _config.MaxNumberOfMessages,
                        MessageAttributeNames = _config.MessageAttributeNames,
                        QueueUrl = queueUrl
                    }, cancellationToken);

                var messagesCount = result.Messages?.Count ?? 0;
                _logger.LogTrace("{count} messages received", messagesCount);
                return result.Messages ?? [];
            }
            catch (Exception e)
            {
                if (_config.OnGetMessagesException is null)
                {
                    _logger.Log(_config.ExceptionDefaultMessageLogLevel, e, "Failed to receive messages from the queue");
                }
                else
                {
                    _config.OnGetMessagesException(e);
                }

                return [];
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var tasks = new List<Task>();
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
                tasks.Add(ProcessEvent(message, cancellationToken, semaphore, queueUrl));

                if (!cancellationToken.IsCancellationRequested)
                {
                    continue;
                }
                
                await FinishTasks(tasks);
                return;
            }

            await FinishTasks(tasks);
        }

        private static async Task FinishTasks(IReadOnlyCollection<Task> tasks)
        {
            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        private async Task ProcessEvent(Message message, CancellationToken cancellationToken, SemaphoreSlim semaphore, string queueUrl)
        {
            try
            {
                await _consumerResolver.Resolve(message, cancellationToken);
                await DeleteMessage(queueUrl, message, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning(e,
                    "Failed to handle message with id {message_id} and ReceiptHandle {receipt_handle}. Task has been cancelled");
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to handle message with id {message_id} and ReceiptHandle {receipt_handle}");
            }
            finally
            {
                semaphore.Release();
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

                var messagesCount = result.Messages.Count;
                _logger.LogTrace("{count} messages received", messagesCount);
                return result.Messages;
            }
            catch (OperationCanceledException e)
            {
                _logger.LogWarning(e,
                    "Failed to delete message with id {message_id} and ReceiptHandle {receipt_handle}. Task has been cancelled");
                return Enumerable.Empty<Message>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to receive messages from the queue"); ;
                return Enumerable.Empty<Message>();
            }
        }

        private async Task DeleteMessage(
            string queueUrl, Message message, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogTrace("Deleting the message with id {message_id} and ReceiptHandle {receipt_handle}");
                await _amazonSqsClient.DeleteMessageAsync(
                        new DeleteMessageRequest
                        {
                            QueueUrl = queueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message {message_id} from the queue", message.MessageId);
            }
        }
    }
}
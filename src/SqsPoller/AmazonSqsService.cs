using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller
{
    public class AmazonSqsService
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly SqsPollerConfig _sqsPollerConfig;
        private readonly IQueueUrlResolver _queueUrlResolver;

        public AmazonSqsService(IOptions<SqsPollerConfig> sqsPollerConfig, IAmazonSQS amazonSqs,
            IQueueUrlResolver queueUrlResolver)
        {
            _sqsPollerConfig = sqsPollerConfig.Value;
            _queueUrlResolver = queueUrlResolver;
            _amazonSqsClient = amazonSqs;
        }

        public async Task<ReceiveMessageResponse> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            var queueUrl = await _queueUrlResolver.Resolve(cancellationToken);

            var receiveMessageRequest = new ReceiveMessageRequest
            {
                WaitTimeSeconds = _sqsPollerConfig.WaitTimeSeconds,
                MaxNumberOfMessages = _sqsPollerConfig.MaxNumberOfMessages,
                MessageAttributeNames = _sqsPollerConfig.MessageAttributeNames,
                QueueUrl = queueUrl
            };

            return await _amazonSqsClient.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
        }

        public async Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default)
        {
            var queueUrl = await _queueUrlResolver.Resolve(cancellationToken);
            await _amazonSqsClient.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle
            }, cancellationToken);
        }
    }
}
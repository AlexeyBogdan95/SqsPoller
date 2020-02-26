using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using SqsPoller.Abstractions;

namespace SqsPoller
{
    internal class AmazonSqsReciever
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly SqsPollerConfig _sqsPollerConfig;

        public AmazonSqsReciever(SqsPollerConfig sqsPollerConfig, IAmazonSQS amazonSqs)
        {
            _sqsPollerConfig = sqsPollerConfig;
            _amazonSqsClient = amazonSqs;
        }

        public async Task<ReceiveMessageResponse> ReceiveMessageAsync(CancellationToken cancellationToken = default)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = _sqsPollerConfig.QueueUrl,
                WaitTimeSeconds = _sqsPollerConfig.WaitTimeSeconds,
                MaxNumberOfMessages = _sqsPollerConfig.MaxNumberOfMessages,
                MessageAttributeNames = _sqsPollerConfig.MessageAttributeNames,
            };

            return await _amazonSqsClient.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
        }

        public async Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default)
        {
            await _amazonSqsClient.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = _sqsPollerConfig.QueueUrl,
                ReceiptHandle = receiptHandle
            }, cancellationToken);
        }
    }
}
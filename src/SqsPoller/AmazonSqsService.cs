using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SqsPoller.Resolvers;

namespace SqsPoller
{
    public class AmazonSqsService
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly SqsPollerConfig _sqsPollerConfig;
        private readonly IQueueUrlResolver _queueUrlResolver;

        public AmazonSqsService(SqsPollerConfig sqsPollerConfig, IAmazonSQS amazonSqs, IQueueUrlResolver queueUrlResolver)
        {
            _sqsPollerConfig = sqsPollerConfig;
            _queueUrlResolver = queueUrlResolver;
            _amazonSqsClient = amazonSqs;
        }
        
        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            var queueUrl = await _queueUrlResolver.Resolve(cancellationToken);
            await _amazonSqsClient.SendMessageAsync(queueUrl, JsonConvert.SerializeObject(message), cancellationToken);
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
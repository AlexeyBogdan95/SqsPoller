using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.Publisher
{
    public class AmazonSqsPublisher
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly IQueueUrlResolver _queueUrlResolver;

        public AmazonSqsPublisher(IAmazonSQS amazonSqs, IQueueUrlResolver queueUrlResolver)
        {
            _queueUrlResolver = queueUrlResolver;
            _amazonSqsClient = amazonSqs;
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            var queueUrl = await _queueUrlResolver.Resolve(cancellationToken);
            await _amazonSqsClient.SendMessageAsync(new SendMessageRequest()
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(message),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        Constants.MessageType, new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = message.GetType().FullName
                        }
                    }
                }
            }, cancellationToken);
        }
    }
}
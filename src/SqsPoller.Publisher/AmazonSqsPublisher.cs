using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SqsPoller.Abstractions;

namespace SqsPoller.Publisher
{
    public class AmazonSqsPublisher
    {
        private readonly IAmazonSQS _amazonSqsClient;

        public AmazonSqsPublisher(IAmazonSQS amazonSqs)
        {
            _amazonSqsClient = amazonSqs;
        }

        public async Task PublishAsync<T>(string queueUrl, T message, CancellationToken cancellationToken = default)
            where T : class, new()
        {
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
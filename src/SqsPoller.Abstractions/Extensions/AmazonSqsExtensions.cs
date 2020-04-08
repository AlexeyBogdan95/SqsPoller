using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace SqsPoller.Abstractions.Extensions
{
    public static class AmazonSqsExtensions
    {
        public static async Task PublishAsync<T>(this IAmazonSQS amazonSqsClient, string queueUrl, T message,
            Dictionary<string, MessageAttributeValue> messageAttributes = default, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            if (messageAttributes == default)
            {
                messageAttributes = new Dictionary<string, MessageAttributeValue>();
            } 
            
            if (!messageAttributes.ContainsKey(Constants.MessageType))
            {
                messageAttributes.Add(Constants.MessageType, new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = message.GetType().FullName
                });
            }
            
            await amazonSqsClient.SendMessageAsync(new SendMessageRequest()
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(message),
                MessageAttributes = messageAttributes
            }, cancellationToken);
        }
    }
}
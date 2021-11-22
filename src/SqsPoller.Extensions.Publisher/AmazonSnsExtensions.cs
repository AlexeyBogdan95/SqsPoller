using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace SqsPoller.Extensions.Publisher
{
    public static class AmazonSnsExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        public static Task<PublishResponse> PublishAsync<T>(
            this IAmazonSimpleNotificationService client, string topicArn, T message) where T: new()
        {
            return client.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message, _options),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "MessageType", new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = message?.GetType().Name
                        }
                    }
                }
            });
        }
        
        public static Task<PublishResponse> PublishAsync<T>(
            this IAmazonSimpleNotificationService client, string topicArn, T message, Dictionary<string, MessageAttributeValue> messageAttributes) where T: new()
        {
            if (!messageAttributes.ContainsKey("MessageType"))
            {
                messageAttributes.Add("MessageType", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = message?.GetType().Name
                });
            }
            
            return client.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonSerializer.Serialize(message, _options),
                MessageAttributes = messageAttributes
            });
        }
    }
}
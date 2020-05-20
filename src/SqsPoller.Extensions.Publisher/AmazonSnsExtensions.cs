using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SqsPoller.Extensions.Publisher
{
    public static class AmazonSnsExtensions
    {
        public static async Task PublishAsync<T>(
            this IAmazonSimpleNotificationService client, string topicArn, T message) where T: new()
        {
            await client.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = JsonConvert.SerializeObject(message, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }),
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
            
    }
}
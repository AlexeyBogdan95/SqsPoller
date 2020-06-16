using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SqsPoller.Extensions.Publisher
{
    public static class AmazonSqsExtensions
    {
        public static async Task<SendMessageResponse> SendMessageAsync<T>(
            this IAmazonSQS amazonSqsClient, 
            string queueUrl,
            T message, 
            CancellationToken cancellationToken = default) where T: new()
        {
            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(message, new JsonSerializerSettings
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
            }, cancellationToken);
        }
    }
}
using System;
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
            var payload = JsonConvert.SerializeObject(
                message, 
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var attributes = new Dictionary<string, MessageAttribute>
            {
                {
                    "MessageType", new MessageAttribute
                    {
                        Type = "String",
                        Value = message?.GetType().Name
                    }
                }
            };
            
            
            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(new
                {
                    Messsage = payload,
                    MessageAttributes = attributes
                })
            }, cancellationToken);
        }

        public static async Task<SendMessageResponse> SendMessageAsync(
            this IAmazonSQS amazonSqsClient,
            string queueUrl,
            object message,
            Type type,
            CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(
                message, 
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

            var attributes = new Dictionary<string, MessageAttribute>
            {
                {
                    "MessageType", new MessageAttribute
                    {
                        Type = "String",
                        Value = type.Name
                    }
                }
            };
            
            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(new
                {
                    Messsage = payload,
                    MessageAttributes = attributes
                })
            }, cancellationToken);
        }
    }
}
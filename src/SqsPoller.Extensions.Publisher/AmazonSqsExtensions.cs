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
            int delayInSeconds = 0,
            CancellationToken cancellationToken = default) where T: new()
        {
            var messageBody = new MessageBody
            {
                Message = JsonConvert.SerializeObject(
                    message,
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }),
                MessageAttributes = new Dictionary<string, MessageAttribute>
                {
                    {
                        "MessageType", new MessageAttribute
                        {
                            Type = "String",
                            Value = message?.GetType().Name
                        }
                    }
                }
            };

            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(messageBody),
                DelaySeconds = delayInSeconds
            }, cancellationToken);
        }

        public static async Task<SendMessageResponse> SendMessageAsync(
            this IAmazonSQS amazonSqsClient,
            string queueUrl,
            object message,
            Type type,
            int delayInSeconds = 0,
            CancellationToken cancellationToken = default)
        {
            var messageBody = new MessageBody
            {
                Message = JsonConvert.SerializeObject(
                    message,
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }),
                MessageAttributes = new Dictionary<string, MessageAttribute>
                {
                    {
                        "MessageType", new MessageAttribute
                        {
                            Type = "String",
                            Value = type.Name
                        }
                    }
                }
            };
            
            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(messageBody),
                DelaySeconds = delayInSeconds
            }, cancellationToken);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
namespace SqsPoller.Extensions.Publisher
{
    public static class AmazonSqsExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        public static Task<SendMessageResponse> SendMessageAsync<T>(
            this IAmazonSQS amazonSqsClient,
            string queueUrl,
            T message,
            int delayInSeconds = 0,
            CancellationToken cancellationToken = default) where T: new()
        {
            var messageBody = new MessageBody
            {
                Message = JsonSerializer.Serialize(
                    message,
                    _options),
                MessageAttributes = new Dictionary<string, MessageAttribute>
                {
                    {
                        "MessageType", new MessageAttribute
                        {
                            Type = "String",
                            Value = message!.GetType().Name
                        }
                    }
                }
            };

            return amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(messageBody),
                DelaySeconds = delayInSeconds
            }, cancellationToken);
        }

        public static Task<SendMessageResponse> SendMessageAsync(
            this IAmazonSQS amazonSqsClient,
            string queueUrl,
            object message,
            Type type,
            int delayInSeconds = 0,
            CancellationToken cancellationToken = default)
        {
            var messageBody = new MessageBody
            {
                Message = JsonSerializer.Serialize(
                    message,
                    _options),
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
            
            return amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(messageBody),
                DelaySeconds = delayInSeconds
            }, cancellationToken);
        }
    }
}
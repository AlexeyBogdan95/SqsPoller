using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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
            bool compress = false,
            CancellationToken cancellationToken = default) where T : new()
        {
            var messageBody = await GetMessageBody(message, message?.GetType(), compress);

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
            bool compress = false,
            CancellationToken cancellationToken = default)
        {
            var messageBody = await GetMessageBody(message, type, compress);

            return await amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonConvert.SerializeObject(messageBody),
                DelaySeconds = delayInSeconds
            }, cancellationToken);
        }

        private static async Task<MessageBody> GetMessageBody(object? message, Type? type, bool compress)
        {
            var messageBody = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var messageAttributes = new Dictionary<string, MessageAttribute>
            {
                {
                    "MessageType", new MessageAttribute
                    {
                        Type = "String",
                        Value = type?.Name
                    }
                }
            };

            if (compress)
            {
                var bytes = Encoding.UTF8.GetBytes(messageBody);
                using var compressedStream = new MemoryStream();
                using (var gzip = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    await gzip.WriteAsync(bytes, 0, bytes.Length);
                }

                messageBody = Convert.ToBase64String(compressedStream.ToArray());
                messageAttributes.Add("ContentEncoding", new MessageAttribute
                {
                    Type = "String",
                    Value = "gzip"
                });
            }

            return new MessageBody
            {
                Message = messageBody,
                MessageAttributes = messageAttributes
            };
        }
    }
}
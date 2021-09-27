using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SqsPoller.Extensions.Publisher
{
    public static class AmazonSnsExtensions
    {
        public static async Task<PublishResponse> PublishAsync<T>(
            this IAmazonSimpleNotificationService client, string topicArn, T message, bool compress = false) where T : new()
        {
            var messageBody = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var messageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "MessageType", new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = message?.GetType().Name
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
                messageAttributes.Add("ContentEncoding", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "gzip"
                });
            }

            return await client.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody,
                MessageAttributes = messageAttributes
            });
        }
    }
}
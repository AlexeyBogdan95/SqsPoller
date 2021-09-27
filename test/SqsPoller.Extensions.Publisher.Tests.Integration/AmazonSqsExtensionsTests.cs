using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace SqsPoller.Extensions.Publisher.Tests.Integration
{
    public class AmazonSqsExtensionsTests
    {
        [Fact]
        public async Task SendMessage_HasMessageTypeAttributeAndBody()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString();
            var firstMessage = new FirstMessage {Value = "value1"};
            var secondMessage = new SecondMessage {Value = "value2"};
            var thirdMessage = new ThirdMessage {Value = "value3"};

            var sqsClient = new AmazonSQSClient(
                AppConfig.AccessKey,
                AppConfig.SecretKey,
                new AmazonSQSConfig {ServiceURL = AppConfig.ServiceUrl});

            await sqsClient.CreateQueueAsync(queueName);
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

            //Act
            await sqsClient.SendMessageAsync(queueUrl, firstMessage);
            await sqsClient.SendMessageAsync(queueUrl, secondMessage);
            await sqsClient.SendMessageAsync(queueUrl, thirdMessage, typeof(ThirdMessage));
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 3,
                MessageAttributeNames = new List<string> {"All"}
            });
            
            var messageTypes = response.Messages
                .Select(message => JsonConvert.DeserializeObject<MessageBody>(message.Body))
                .Select(body => body.MessageAttributes.Single(pair => pair.Key == "MessageType").Value.Value)
                .ToList();

            var messages = response.Messages
                .Select(message => JsonConvert.DeserializeObject<MessageBody>(message.Body))
                .Select(body => body.Message)
                .ToArray();

            //Assert
            new[] {nameof(FirstMessage), nameof(SecondMessage), nameof(ThirdMessage)}
                .ShouldAllBe(s => messageTypes.Contains(s));
            
            new [] {firstMessage.Value, secondMessage.Value, thirdMessage.Value}
                .ShouldAllBe(s => messages.Any(m => m.Contains(s)));
        }

        [Fact]
        public async Task SendMessage_Compressed_HasMessageTypeAttributeAndEncodedBody()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString();
            var firstMessage = new FirstCompressedMessage { FirstValue = "Some Test Value", SecondValue = 23 };
            var secondMessage = new SecondCompressedMessage { FirstValue = new DateTime(2021, 12, 12), SecondValue = 4.53m };
            var firstMessageEncodedBody = "H4sIAAAAAAAACqvm5VJQUErLLCouCUvMKU1VslJQCs7PTVUISS0uUYAI6YDVFKcm5+elwBQZGfNy1QIAMWRysz0AAAA=";
            var secondMessageEncodedBody = "H4sIAAAAAAAACqvm5VJQUErLLCouCUvMKU1VslJQMjIwMtQ1NAKiEAMDKzBS0gGrK05Nzs9LgSk00TM15uWqBQDCQr7xQwAAAA==";

            var sqsClient = new AmazonSQSClient(
                AppConfig.AccessKey,
                AppConfig.SecretKey,
                new AmazonSQSConfig { ServiceURL = AppConfig.ServiceUrl });

            await sqsClient.CreateQueueAsync(queueName);
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

            //Act
            await sqsClient.SendMessageAsync(queueUrl, firstMessage, compress: true);
            await sqsClient.SendMessageAsync(queueUrl, secondMessage, typeof(SecondCompressedMessage), compress: true);
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 2,
                MessageAttributeNames = new List<string> { "All" }
            });

            var messageTypes = response.Messages
                .Select(message => JsonConvert.DeserializeObject<MessageBody>(message.Body))
                .Select(body => body.MessageAttributes.Single(pair => pair.Key == "MessageType").Value.Value)
                .ToList();

            var messages = response.Messages
                .Select(message => JsonConvert.DeserializeObject<MessageBody>(message.Body))
                .Select(body => body.Message)
                .ToArray();

            //Assert
            new[] { nameof(FirstCompressedMessage), nameof(SecondCompressedMessage) }
                .ShouldAllBe(s => messageTypes.Contains(s));

            new[] { firstMessageEncodedBody, secondMessageEncodedBody }
                .ShouldAllBe(s => messages.Any(m => m.Contains(s)));
        }
    }
}
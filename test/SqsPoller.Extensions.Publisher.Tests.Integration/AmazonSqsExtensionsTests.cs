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
            var sqsClient = new AmazonSQSClient(
                AppConfig.AccessKey,
                AppConfig.SecretKey,
                new AmazonSQSConfig {ServiceURL = AppConfig.ServiceUrl});

            await sqsClient.CreateQueueAsync(queueName);
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

            //Act
            await sqsClient.SendMessageAsync(queueUrl, firstMessage);
            await sqsClient.SendMessageAsync(queueUrl, secondMessage);
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 2,
                MessageAttributeNames = new List<string> {"All"}
            });
            
            var messageTypes = response.Messages
                .Select(message => JsonConvert.DeserializeObject<MessageBody>(message.Body))
                .Select(body => body.MessageAttributes.Single(pair => pair.Key == "MessageType").Value.Value)
                .ToList();

            //Assert
            new[] {nameof(FirstMessage), nameof(SecondMessage)}
                .ShouldAllBe(s => messageTypes.Contains(s));
            new [] {firstMessage.Value, secondMessage.Value}
                .ShouldAllBe(s => response.Messages.Any(message => message.Body.Contains(s)));
        }
    }
}
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
    }
}
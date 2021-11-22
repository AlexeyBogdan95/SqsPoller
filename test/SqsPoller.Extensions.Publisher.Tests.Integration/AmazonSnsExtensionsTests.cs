using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace SqsPoller.Extensions.Publisher.Tests.Integration
{
    public class AmazonSnsExtensionsTests
    {
        [Fact]
        public async Task Publish_HasMessageTypeAttributeAndBody()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString();
            var topicName = Guid.NewGuid().ToString();
            var firstMessage = new FirstMessage {Value = "value1"};
            var secondMessage = new SecondMessage {Value = "value2"};

            var snsClient = new AmazonSimpleNotificationServiceClient(
                AppConfig.AccessKey, 
                AppConfig.SecretKey, 
                new AmazonSimpleNotificationServiceConfig {
                    ServiceURL = AppConfig.ServiceUrl
                });

            var topicArn = (await snsClient.CreateTopicAsync(topicName)).TopicArn;
            var sqsClient = new AmazonSQSClient(
                AppConfig.AccessKey,
                AppConfig.SecretKey,
                new AmazonSQSConfig {ServiceURL = AppConfig.ServiceUrl});
            await sqsClient.CreateQueueAsync(queueName);
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

            await snsClient.SubscribeQueueAsync(topicArn, sqsClient, queueUrl);

            //Act
            await snsClient.PublishAsync(topicArn, firstMessage);
            await snsClient.PublishAsync(topicArn, secondMessage);
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
        
        [Fact]
        public async Task Publish_HasMessageTypeAttributeAndBodyWithCustomAttributes()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString();
            var topicName = Guid.NewGuid().ToString();
            var firstMessage = new FirstMessage {Value = "value1"};
            var secondMessage = new SecondMessage {Value = "value2"};

            var snsClient = new AmazonSimpleNotificationServiceClient(
                AppConfig.AccessKey, 
                AppConfig.SecretKey, 
                new AmazonSimpleNotificationServiceConfig {
                    ServiceURL = AppConfig.ServiceUrl
                });

            var topicArn = (await snsClient.CreateTopicAsync(topicName)).TopicArn;
            var sqsClient = new AmazonSQSClient(
                AppConfig.AccessKey,
                AppConfig.SecretKey,
                new AmazonSQSConfig {ServiceURL = AppConfig.ServiceUrl});
            await sqsClient.CreateQueueAsync(queueName);
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

            await snsClient.SubscribeQueueAsync(topicArn, sqsClient, queueUrl);

            //Act
            await snsClient.PublishAsync(topicArn, firstMessage, new Dictionary<string, MessageAttributeValue>());
            await snsClient.PublishAsync(topicArn, secondMessage, new Dictionary<string, MessageAttributeValue>());
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
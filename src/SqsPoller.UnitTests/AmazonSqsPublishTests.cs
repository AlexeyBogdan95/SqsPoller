using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Resolvers;
using SqsPoller.Abstractions.Extensions;

namespace SqsPoller.UnitTests
{
    [TestClass]
    public class AmazonSqsPublisherTests
    {
        private const string QueueName = "queue";
        private string QueueUrl => $"https://sqs.us-east-1.amazonaws.com/exampleaccoundid/queueu/{QueueName}";
        
        private class TestMessage
        {
            public string TestProperty { get; set; }
        }

        [TestMethod]
        public async Task TestPublishAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            var testMessage = new TestMessage() {TestProperty = "prop"};
            
            await sqsMock.Object.PublishAsync(QueueUrl, testMessage);

            sqsMock.Verify(sqs => sqs.SendMessageAsync(
                It.Is<SendMessageRequest>(request => request.QueueUrl == QueueUrl && request.MessageBody == "{\"TestProperty\":\"prop\"}"
                    && request.MessageAttributes.Count == 1 && request.MessageAttributes[Constants.MessageType].StringValue == typeof(TestMessage).FullName),
                It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task TestPublishWithAwsAccountUrlAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.GetQueueUrlAsync(It.IsAny<GetQueueUrlRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateQueueUrlResponse()));

            var testMessage = new TestMessage() {TestProperty = "prop"};

            var resolver = new AwsAccountQueueUrlResolver(sqsMock.Object);
            var queueName = await resolver.Resolve(QueueName);
            await sqsMock.Object.PublishAsync(queueName, testMessage);

            sqsMock.Verify(sqs => sqs.SendMessageAsync(
                It.Is<SendMessageRequest>(request => request.QueueUrl == QueueUrl && request.MessageBody == "{\"TestProperty\":\"prop\"}"
                    && request.MessageAttributes.Count == 1 && request.MessageAttributes[Constants.MessageType].StringValue == typeof(TestMessage).FullName),
                It.IsAny<CancellationToken>()));
        }

        private GetQueueUrlResponse CreateQueueUrlResponse()
        {
            return Builder<GetQueueUrlResponse>
                .CreateNew()
                .With(response => response.QueueUrl = QueueUrl)
                .Build();
        }
    }
}
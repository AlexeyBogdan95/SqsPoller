using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using FizzWare.NBuilder;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.Publisher.UnitTests
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
            var config = Options.Create(new SqsPollerConfig() {QueueUrl = QueueUrl});
            var sqsMock = new Mock<IAmazonSQS>();
            var service = new AmazonSqsPublisher(sqsMock.Object, new DefaultQueueUrlResolver(config));
            var testMessage = new TestMessage() {TestProperty = "prop"};
            
            await service.PublishAsync(testMessage);

            sqsMock.Verify(b => b.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.QueueUrl == QueueUrl && r.MessageBody == "{\"TestProperty\":\"prop\"}"
                    && r.MessageAttributes.Count == 1 && r.MessageAttributes[Constants.MessageType].StringValue == typeof(TestMessage).FullName),
                It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task TestPublishWithAwsAccountUrlAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.GetQueueUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateQueueUrlResponse()));

            var service = new AmazonSqsPublisher(sqsMock.Object, new AwsAccountQueueUrlResolver(sqsMock.Object, QueueName));
            var testMessage = new TestMessage() {TestProperty = "prop"};

            await service.PublishAsync(testMessage);

            sqsMock.Verify(b => b.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.QueueUrl == QueueUrl && r.MessageBody == "{\"TestProperty\":\"prop\"}"
                    && r.MessageAttributes.Count == 1 && r.MessageAttributes[Constants.MessageType].StringValue == typeof(TestMessage).FullName),
                It.IsAny<CancellationToken>()));
        }

        private GetQueueUrlResponse CreateQueueUrlResponse()
        {
            return Builder<GetQueueUrlResponse>
                .CreateNew()
                .With(it => it.QueueUrl = QueueUrl)
                .Build();
        }
    }
}
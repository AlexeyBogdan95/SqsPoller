using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqsPoller.Resolvers;

namespace SqsPoller.UnitTests
{
    [TestClass]
    public class AmazonSqsServiceTests
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
            var config = new SqsPollerConfig() {QueueUrl = QueueUrl};
            var sqsMock = new Mock<IAmazonSQS>();
            var service = new AmazonSqsService(config, sqsMock.Object, new DefaultQueueUrlResolver(config));
            var testMessage = new TestMessage() {TestProperty = "prop"};
            
            await service.PublishAsync(testMessage);

            sqsMock.Verify(b => b.SendMessageAsync(QueueUrl, "{\"TestProperty\":\"prop\"}", It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task TestPublishWithAwsAccountUrlAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.GetQueueUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateQueueUrlResponse()));

            var service = new AmazonSqsService(new SqsPollerConfig(), sqsMock.Object, new AwsAccountQueueUrlResolver(sqsMock.Object, QueueName));
            var testMessage = new TestMessage() {TestProperty = "prop"};

            await service.PublishAsync(testMessage);

            sqsMock.Verify(b => b.SendMessageAsync(QueueUrl, "{\"TestProperty\":\"prop\"}", It.IsAny<CancellationToken>()));
        }
        
        [TestMethod]
        public async Task TestReceiveMessageAsync()
        {
            var config = new SqsPollerConfig() {QueueUrl = QueueUrl};
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateReceiveMessageResponse()));

            var service = new AmazonSqsService(config, sqsMock.Object, new DefaultQueueUrlResolver(config));

            await service.ReceiveMessageAsync();

            sqsMock.Verify(b => b.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(r => r.QueueUrl == QueueUrl), It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task TestReceiveMessageWithAwsAccountUrlAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.GetQueueUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateQueueUrlResponse()));
            sqsMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateReceiveMessageResponse()));

            var service = new AmazonSqsService(new SqsPollerConfig(), sqsMock.Object, new AwsAccountQueueUrlResolver(sqsMock.Object, QueueName));

            await service.ReceiveMessageAsync();

            sqsMock.Verify(b => b.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(r => r.QueueUrl == QueueUrl), It.IsAny<CancellationToken>()));
        }
        
        private GetQueueUrlResponse CreateQueueUrlResponse()
        {
            return Builder<GetQueueUrlResponse>
                .CreateNew()
                .With(it => it.QueueUrl = QueueUrl)
                .Build();
        }
        
        private ReceiveMessageResponse CreateReceiveMessageResponse()
        {
            return Builder<ReceiveMessageResponse>
                .CreateNew()
                .Build();
        }
    }
}
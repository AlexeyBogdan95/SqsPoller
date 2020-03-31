using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.UnitTests
{
    [TestClass]
    public class AwsAccountQueueUrlResolverTests
    {
        private const string QueueName = "queue";
        private string QueueUrl => $"https://sqs.us-east-1.amazonaws.com/exampleaccoundid/queueu/{QueueName}";
        
        [TestMethod]
        public async Task TestPublishWithAwsAccountUrlAsync()
        {
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.GetQueueUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateQueueUrlResponse()));

            var resolver = new AwsAccountQueueUrlResolver(sqsMock.Object);

            Assert.AreEqual(QueueUrl, await resolver.Resolve(QueueName));
            Assert.AreEqual(QueueUrl, await resolver.Resolve(QueueName));
            Assert.AreEqual(QueueUrl, await resolver.Resolve(QueueName));
            
            sqsMock.Verify(b => b.GetQueueUrlAsync(QueueName, It.IsAny<CancellationToken>()), Times.Once);
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
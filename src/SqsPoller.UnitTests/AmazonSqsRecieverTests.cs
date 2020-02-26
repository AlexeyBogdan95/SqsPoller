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

namespace SqsPoller.UnitTests
{
    [TestClass]
    public class AmazonSqsRecieverTests
    {
        private const string QueueName = "queue";
        private string QueueUrl => $"https://sqs.us-east-1.amazonaws.com/exampleaccoundid/queueu/{QueueName}";
        
        [TestMethod]
        public async Task TestReceiveMessageAsync()
        {
            var config = new SqsPollerConfig() {QueueUrl = QueueUrl};
            var sqsMock = new Mock<IAmazonSQS>();
            sqsMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(CreateReceiveMessageResponse()));

            var service = new AmazonSqsReciever(config, sqsMock.Object);

            await service.ReceiveMessageAsync();

            sqsMock.Verify(b => b.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(r => r.QueueUrl == QueueUrl), It.IsAny<CancellationToken>()));
        }
        
        private ReceiveMessageResponse CreateReceiveMessageResponse()
        {
            return Builder<ReceiveMessageResponse>
                .CreateNew()
                .Build();
        }
    }
}
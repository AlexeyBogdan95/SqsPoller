using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SqsPoller.Abstractions;
using SqsPoller.Resolvers;

namespace SqsPoller.UnitTests
{
    [TestClass]
    public class ConsumerResolverTests
    {
        public class TestMessage
        {
            public string TestProperty { get; set; }
        }

        public class SecondTestMessage
        {
            public string TestProperty { get; set; }
        }
        
        [TestMethod]
        public async Task TestResolve()
        {
            var consumerMock = new Mock<IConsumer<TestMessage>>();
            consumerMock
                .Setup(x => x.Consume(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(Task.FromResult(true)));
            
            var resolver = new ConsumerResolver(new[] {consumerMock.Object});
            
            var testMessage = new TestMessage() {TestProperty = "prop"};

            await resolver.Resolve(JsonConvert.SerializeObject(testMessage), typeof(TestMessage).FullName);

            consumerMock.Verify(b => b.Consume(It.Is<TestMessage>(m => m.TestProperty == testMessage.TestProperty), It.IsAny<CancellationToken>()), Times.Once);

            await Assert.ThrowsExceptionAsync<ConsumerNotFoundException>(() => resolver.Resolve(JsonConvert.SerializeObject(testMessage), nameof(TestMessage)));
        }
             
        [TestMethod]
        public async Task TestResolveOneConsumerOnly()
        {
            var consumerMock = new Mock<IConsumer<TestMessage>>();
            consumerMock
                .Setup(x => x.Consume(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(Task.FromResult(true)));

            var secondConsumerMock = new Mock<IConsumer<SecondTestMessage>>();
            
            var resolver = new ConsumerResolver(new IConsumer[] {consumerMock.Object, secondConsumerMock.Object});
            
            var testMessage = new TestMessage() {TestProperty = "prop"};

            await resolver.Resolve(JsonConvert.SerializeObject(testMessage), typeof(TestMessage).FullName);

            consumerMock.Verify(b => b.Consume(It.Is<TestMessage>(m => m.TestProperty == testMessage.TestProperty), It.IsAny<CancellationToken>()), Times.Once);
            secondConsumerMock.Verify(b => b.Consume(It.IsAny<SecondTestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
             
        [TestMethod]
        public async Task TestResolveWithSpecifiedConsumers()
        {
            var consumerMock = new Mock<IConsumer<TestMessage>>();
            consumerMock
                .Setup(x => x.Consume(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(true));

            var secondConsumerMock = new Mock<IConsumer<SecondTestMessage>>();

            var resolver = new ConsumerResolver(new IConsumer[] {consumerMock.Object, secondConsumerMock.Object},
                new[] {secondConsumerMock.Object.GetType()});
            
            var testMessage = new TestMessage() {TestProperty = "prop"};

            await Assert.ThrowsExceptionAsync<ConsumerNotFoundException>(() => resolver.Resolve(JsonConvert.SerializeObject(testMessage), typeof(TestMessage).FullName));

            consumerMock.Verify(b => b.Consume(It.Is<TestMessage>(m => m.TestProperty == testMessage.TestProperty), It.IsAny<CancellationToken>()), Times.Never);
            secondConsumerMock.Verify(b => b.Consume(It.IsAny<SecondTestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
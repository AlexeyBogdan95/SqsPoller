using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using Xunit;

namespace SqsPoller.Tests.Unit
{
    public class ConsumerResolverTests
    {
        [Fact]
        public async Task Resolve_FirstMessageConsumerFound_HandleMethodIsInvoked()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers);
            var message = new FirstMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = nameof(FirstMessage);

            //Act
            await consumerResolver.Resolve(messageAsString, messageType, CancellationToken.None);

            //Assert
            fakeService.Received(1).FirstMethod();
            fakeService.DidNotReceive().SecondMethod();
        }
        
        [Fact]
        public async Task Resolve_SecondMessageConsumerFound_HandleMethodIsInvoked()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers);
            var message = new SecondMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = nameof(SecondMessage);

            //Act
            await consumerResolver.Resolve(messageAsString, messageType, CancellationToken.None);

            //Assert
            fakeService.DidNotReceive().FirstMethod();
            fakeService.Received(1).SecondMethod();
        }
        
        [Fact]
        public void Resolve_ConsumerNotFound_ConsumerNotFoundExceptionThrown()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers);
            var message = new SecondMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = "fakeMessageType";

            //Act
            var task = consumerResolver.Resolve(messageAsString, messageType, CancellationToken.None);

            //Assert
            Should.Throw<ConsumerNotFoundException>(task);
        }
    }
}
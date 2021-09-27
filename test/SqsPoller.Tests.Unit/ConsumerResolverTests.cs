using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
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
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);
            var message = new FirstMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = nameof(FirstMessage);
            var sqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = messageType}}
                },
                Body = messageAsString
            };

            //Act
            await consumerResolver.Resolve(sqsMessage, CancellationToken.None);

            //Assert
            fakeService.Received(1).FirstMethod(Arg.Is(message));
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstCompressedMessage>());
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondMessage>());
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondCompressedMessage>());
        }

        [Fact]
        public async Task Resolve_SecondMessageConsumerFound_HandleMethodIsInvoked()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);
            var message = new SecondMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = nameof(SecondMessage);
            var sqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = messageType}}
                },
                Body = messageAsString
            };

            //Act
            await consumerResolver.Resolve(sqsMessage, CancellationToken.None);

            //Assert
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstMessage>());
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstCompressedMessage>());
            fakeService.Received(1).SecondMethod(Arg.Is(message));
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondCompressedMessage>());
        }

        [Fact]
        public void Resolve_ConsumerNotFound_ConsumerNotFoundExceptionThrown()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);
            var message = new SecondMessage {Value = "First Message"};
            var messageAsString = JsonConvert.SerializeObject(message);
            var messageType = "fakeMessageType";
            var sqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = messageType}}
                },
                Body = messageAsString
            };

            //Act
            var task = consumerResolver.Resolve(sqsMessage, CancellationToken.None);

            //Assert
            Should.Throw<ConsumerNotFoundException>(task);
        }
        
        [Fact]
        public async Task Resolve_ConsumerFound_TwoMethodsAreInvoked()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var thirdConsumer = new ThirdMessageConsumer(fakeService);
            var consumers = new IConsumer[] {thirdConsumer};
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);
            var firstMessage = new FirstMessage {Value = "First Message"};
            var firstSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(FirstMessage)}}
                },
                Body = JsonConvert.SerializeObject(firstMessage)
            };
            
            var secondMessage = new SecondMessage {Value = "Second Message"};
            var secondSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(SecondMessage)}}
                },
                Body = JsonConvert.SerializeObject(secondMessage)
            };

            //Act
            await consumerResolver.Resolve(firstSqsMessage, CancellationToken.None);
            await consumerResolver.Resolve(secondSqsMessage, CancellationToken.None);

            //Assert
            fakeService.Received(1).FirstMethod(Arg.Is(firstMessage));
            fakeService.Received(1).SecondMethod(Arg.Is(secondMessage));
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstCompressedMessage>());
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondCompressedMessage>());
        }
        
        [Fact]
        public async Task Resolve_ConsumersFound_TwoMethodsAreInvokedTwice()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var thirdConsumer = new ThirdMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer, thirdConsumer};
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);
            var firstMessage = new FirstMessage {Value = "First Message"};
            var firstSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(FirstMessage)}}
                },
                Body = JsonConvert.SerializeObject(firstMessage)
            };
            
            var secondMessage = new SecondMessage {Value = "Second Message"};
            var secondSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(SecondMessage)}}
                },
                Body = JsonConvert.SerializeObject(secondMessage)
            };

            //Act
            await consumerResolver.Resolve(firstSqsMessage, CancellationToken.None);
            await consumerResolver.Resolve(secondSqsMessage, CancellationToken.None);

            //Assert
            fakeService.Received(2).FirstMethod(Arg.Is(firstMessage));
            fakeService.Received(2).SecondMethod(Arg.Is(secondMessage));
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstCompressedMessage>());
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondCompressedMessage>());
        }

        [Fact]
        public async Task Resolve_ConsumersFound_TwoMethodsAreInvokedWithDecompressedParams()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstCompressedMessageConsumer(fakeService);
            var secondConsumer = new SecondCompressedMessageConsumer(fakeService);
            var consumers = new IConsumer[] { firstConsumer, secondConsumer };
            var consumerResolver = new ConsumerResolver(consumers, fakeLogger);

            var firstMessage = new FirstCompressedMessage { FirstValue = "Some Test Value", SecondValue = 23 };
            var firstMessageEncodedBody = "H4sIAAAAAAAACqvm5VJQUErLLCouCUvMKU1VslJQCs7PTVUISS0uUYAI6YDVFKcm5+elwBQZGfNy1QIAMWRysz0AAAA=";
            var firstSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(FirstCompressedMessage)}},
                    {"ContentEncoding", new MessageAttributeValue{StringValue = "gzip"}}
                },
                Body = firstMessageEncodedBody
            };

            var secondMessage = new SecondCompressedMessage { FirstValue = new DateTime(2021, 12, 12), SecondValue = 4.53m };
            var secondMessageEncodedBody = "H4sIAAAAAAAACqvm5VJQUErLLCouCUvMKU1VslJQMjIwMtQ1NAKiEAMDKzBS0gGrK05Nzs9LgSk00TM15uWqBQDCQr7xQwAAAA==";
            var secondSqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = nameof(SecondCompressedMessage)}},
                    {"ContentEncoding", new MessageAttributeValue{StringValue = "gzip"}}
                },
                Body = secondMessageEncodedBody
            };

            //Act
            await consumerResolver.Resolve(firstSqsMessage, CancellationToken.None);
            await consumerResolver.Resolve(secondSqsMessage, CancellationToken.None);

            //Assert
            fakeService.DidNotReceive().FirstMethod(Arg.Any<FirstMessage>());
            fakeService.DidNotReceive().SecondMethod(Arg.Any<SecondMessage>());
            fakeService.Received(1).FirstMethod(Arg.Is(firstMessage));
            fakeService.Received(1).SecondMethod(Arg.Is(secondMessage));
        }
    }
}
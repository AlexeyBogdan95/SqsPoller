using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
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
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            fakeService.Received(1).FirstMethod(message.Value);
            fakeService.DidNotReceive().SecondMethod(string.Empty);
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
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            fakeService.DidNotReceive().FirstMethod(string.Empty);
            fakeService.Received(1).SecondMethod(message.Value);
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
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            fakeService.Received(1).FirstMethod(firstMessage.Value);
            fakeService.Received(1).SecondMethod(secondMessage.Value);
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
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            fakeService.Received(2).FirstMethod(firstMessage.Value);
            fakeService.Received(2).SecondMethod(secondMessage.Value);
        }
        
        [Fact]
        public async Task Resolve_ConsumersFound_HandleMethodIsInvoked()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var firstConsumer = new FirstMessageConsumer(fakeService);
            var secondConsumer = new SecondMessageConsumer(fakeService);
            var consumers = new IConsumer[] {firstConsumer, secondConsumer};
            var serviceCollection = new ServiceCollection();
            foreach (var consumer in consumers)
            {
                serviceCollection.AddTransient(consumer.GetType(), _ => consumer);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = consumers.Select(x => x.GetType()).ToArray();
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
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
            fakeService.Received(1).FirstMethod(message.Value);
            fakeService.DidNotReceive().SecondMethod(string.Empty);
        }

        [Fact]
        public async Task Resolve_ConsumerFound_HandleEnumAsString()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var enumConsumer = new EnumMessageConsumer(fakeService);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(enumConsumer.GetType(), _ => enumConsumer);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = new [] { enumConsumer.GetType() };
            var jsonConverter = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger, jsonConverter);
            var message = new EnumMessage { Value = SampleEnum.Value };
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            var messageAsString = System.Text.Json.JsonSerializer.Serialize(message, jsonSerializerOptions);
            var messageType = nameof(EnumMessage);
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
            fakeService.Received(1).EnumMethod(message.Value);
        }

        [Fact]
        public async Task Resolve_ConsumerFound_ExpectExceptionEnumAsString()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var enumConsumer = new EnumMessageConsumer(fakeService);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(enumConsumer.GetType(), _ => enumConsumer);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = new [] { enumConsumer.GetType() };
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
            var message = new EnumMessage { Value = SampleEnum.Value };
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            var messageAsString = System.Text.Json.JsonSerializer.Serialize(message, jsonSerializerOptions);
            var messageType = nameof(EnumMessage);
            var sqsMessage = new Message
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"MessageType", new MessageAttributeValue {StringValue = messageType}}
                },
                Body = messageAsString
            };

            //Act
            var act = async () => await consumerResolver.Resolve(sqsMessage, CancellationToken.None);

            //Assert
            await act.ShouldThrowAsync<System.Text.Json.JsonException>();
        }

        [Fact]
        public async Task Resolve_ConsumerFound_HandleEnumAsNumber()
        {
            //Arrange
            var fakeService = Substitute.For<IFakeService>();
            var fakeLogger = Substitute.For<ILogger<ConsumerResolver>>();
            var enumConsumer = new EnumMessageConsumer(fakeService);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(enumConsumer.GetType(), _ => enumConsumer);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var types = new [] { enumConsumer.GetType() };
            var consumerResolver = new ConsumerResolver(serviceProvider, types, fakeLogger);
            var message = new EnumMessage { Value = SampleEnum.Value };
            var messageAsString = System.Text.Json.JsonSerializer.Serialize(message);

            var messageType = nameof(EnumMessage);
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
            fakeService.Received(1).EnumMethod(message.Value);
        }
    }
}
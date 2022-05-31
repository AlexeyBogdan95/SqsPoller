using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace SqsPoller.Tests.Unit;

public class SqsPollerHostedServiceTests
{
    [Fact]
    public async Task HandleMessage_DefaultConfigAndExceptionThrown_LogError()
    {
        //Arrange
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig();
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message()
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.Received(1).Log(LogLevel.Error,
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
    }
    
    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Debug)]
    public async Task HandleMessage_DefaultMessageLogLevelIsSpecifiedAndExceptionThrown_LogWithSpecifiedLogLevel(LogLevel
         logLevel)
    {
        //Arrange
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig
        {
            ExceptionDefaultMessageLogLevel = logLevel
        };
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message()
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.Received(1).Log(logLevel,
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
    }

    [Fact]
    public async Task HandleMessage_OnHandleMessageExceptionIsSpecifiedAndExceptionThrown_LogDefaultMessageAndOnHandleMessageExceptionIsInvoked()
    {
        //Arrange
        bool onExceptionTriggered = false;
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig
        {
            ExceptionDefaultMessageLogLevel = LogLevel.Warning,
            OnHandleMessageException = (_, _) => { onExceptionTriggered = true; }
        };
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message()
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
        onExceptionTriggered.ShouldBeTrue();
    }
    
    [Fact]
    public async Task DeleteMessage_DefaultConfigAndExceptionThrown_LogError()
    {
        //Arrange
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig();
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Returns(Task.CompletedTask);
        sqsClient.DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
        
        logger.Received(1).Log(LogLevel.Error,
            Arg.Any<Exception>(),
            "Failed to delete message {message_id} {receipt_handle}");
    }
    
    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Debug)]
    public async Task DeleteMessage_DefaultMessageLogLevelIsSpecifiedAndExceptionThrown_LogWithSpecifiedLogLevel(LogLevel
        logLevel)
    {
        //Arrange
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig() { ExceptionDefaultMessageLogLevel = logLevel};
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Returns(Task.CompletedTask);
        sqsClient.DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
        
        logger.Received(1).Log(logLevel,
            Arg.Any<Exception>(),
            "Failed to delete message {message_id} {receipt_handle}");
    }
    
    [Fact]
    public async Task DeleteMessage_OnDeleteMessageExceptionIsSpecifiedAndExceptionThrown_LogDefaultMessageAndOnDeleteMessageExceptionIsInvoked()
    {
        //Arrange
        bool onHandleMessageExceptionTriggered = false;
        bool onDeleteMessageExceptionTriggered = false;
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig
        {
            ExceptionDefaultMessageLogLevel = LogLevel.Warning,
            OnHandleMessageException = (_, _) => { onHandleMessageExceptionTriggered = true; },
            OnDeleteMessageException = (_, _) => { onDeleteMessageExceptionTriggered = true; }
        };
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Returns(Task.CompletedTask);
        sqsClient.DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message()
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        }, CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to delte message {message_id} {receipt_handle}");
        
        onHandleMessageExceptionTriggered.ShouldBeFalse();
        onDeleteMessageExceptionTriggered.ShouldBeTrue();
    }
}
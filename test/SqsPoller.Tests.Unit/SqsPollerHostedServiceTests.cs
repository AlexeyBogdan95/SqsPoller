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
        await hostedService.HandleMessage(new Message(), CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.Received(1).Log(
            Arg.Is<LogLevel>(x => x == LogLevel.Error),
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
        await hostedService.HandleMessage(new Message(), CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.Received(1).Log(
            Arg.Is<LogLevel>(x => x == logLevel),
            Arg.Any<Exception>(), 
            "Failed to handle message {message_id} {receipt_handle}");
    }

    [Fact]
    public async Task HandleMessage_OnExceptionIsSpecifiedAndExceptionThrown_LogDefaultMessageAndOnExceptionIsInvoked()
    {
        //Arrange
        bool onExceptionTriggered = false;
        
        var sqsClient = Substitute.For<IAmazonSQS>();
        var config = new SqsPollerConfig
        {
            ExceptionDefaultMessageLogLevel = LogLevel.Warning,
            OnException = (e, m) => { onExceptionTriggered = true; }
        };
        var consumerResolver = Substitute.For<IConsumerResolver>();
        consumerResolver.Resolve(Arg.Any<Message>(), CancellationToken.None).Throws<Exception>();
        var logger = Substitute.For<ILogger<SqsPollerHostedService>>();
        var hostedService = new SqsPollerHostedService(sqsClient, config, consumerResolver, logger);
        
        //Act
        await hostedService.HandleMessage(new Message(), CancellationToken.None, new SemaphoreSlim(20), "");

        //Assert
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<Exception>(),
            "Failed to handle message {message_id} {receipt_handle}");
        onExceptionTriggered.ShouldBeTrue();
    }
        
}
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SqsPoller;
using SqsPoller.Sample.Subscriber;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateLogger();

IHostBuilder hostBuilder = new HostBuilder()
    .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
    {
        configurationBuilder
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

    })
    .ConfigureServices((context, services) =>
    {
        var config = new AppConfig(context.Configuration);
        services.AddSqsPoller(new SqsPollerConfig
        {
            ServiceUrl = config.ServiceUrl,
            QueueName = config.QueueName,
            AccessKey = config.AccessKey,
            SecretKey = config.SecretKey,
            MaxNumberOfMessages = 10,
            MaxNumberOfParallelism = 1000
        }, [
            typeof(BarConsumer),
            typeof(FooConsumer),
            typeof(CustomRouteMessageConsumer),
            typeof(MutableConsumer),
            typeof(CancelConsumer),
            typeof(HttpConsumer),
            typeof(ErrorConsumer)
        ]);

        services.AddSqsPoller(new SqsPollerConfig
        {
            ServiceUrl = config.ServiceUrl,
            QueueUrl = config.SecondQueueUrl,
            AccessKey = config.AccessKey,
            SecretKey = config.SecretKey,
            MaxNumberOfMessages = 10,
            MaxNumberOfParallelism = 1000
        }, [typeof(BarConsumer)]);

        services.AddSqsPoller(new SqsPollerConfig
        {
            ServiceUrl = config.ServiceUrl,
            QueueUrl = config.ThirdQueueUrl,
            AccessKey = config.AccessKey,
            SecretKey = config.SecretKey,
            MaxNumberOfMessages = 10,
            MaxNumberOfParallelism = 1000,
            ExceptionDefaultMessageLogLevel = LogLevel.Information,
            OnHandleMessageException = (exception, messageId) =>
            {
                Console.WriteLine(exception);
                Console.WriteLine($"MessageId: {messageId}");
            }
        }, [typeof(OperationCancelledConsumer)]);
    })
    .UseSerilog()
    .UseConsoleLifetime();

await hostBuilder.RunConsoleAsync();
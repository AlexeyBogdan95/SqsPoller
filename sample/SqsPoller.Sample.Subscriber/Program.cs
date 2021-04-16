using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SqsPoller.Sample.Subscriber
{
    internal class Program
    {
        private static async Task Main()
        {
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
                        MaxNumberOfParallelism = 2
                    }, new[]
                    {
                        typeof(BarConsumer),
                        typeof(FooConsumer),
                        typeof(CustomRouteMessageConsumer),
                        typeof(MutableConsumer),
                        typeof(CancelConsumer),
                        typeof(HttpConsumer),
                        typeof(ErrorConsumer)
                    });

                    services.AddSqsPoller(new SqsPollerConfig
                    {
                        ServiceUrl = config.ServiceUrl,
                        QueueName = config.SecondQueueName,
                        AccessKey = config.AccessKey,
                        SecretKey = config.SecretKey,
                        MaxNumberOfMessages = 10,
                        MaxNumberOfParallelism = 2
                    }, new[] {typeof(BarConsumer)});
                })
                .UseSerilog()
                .UseConsoleLifetime();

            await hostBuilder.RunConsoleAsync();
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.SubscriberTestClient
{
    class Program
    {
        private const string Queue = "TestQueue";

        static async Task Main()
        {
            IHostBuilder hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration((config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
                {
                    configurationBuilder
                        .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    
                    configurationBuilder.Build();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.Configure<SqsPollerConfig>(context.Configuration.GetSection("SQS"));

                    services.AddSqsPoller(
                        sc => sc.GetRequiredService<IOptions<SqsPollerConfig>>().Value,
                        sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), Queue),
                        new [] {typeof(FirstTestConsumer)}
                    );
                    
                    services.AddSqsPoller(
                        sc => sc.GetRequiredService<IOptions<SqsPollerConfig>>().Value,
                        sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), Queue),
                        new [] {typeof(SecondTestConsumer)}
                    );

                    // services.AddSqsPoller(
                    //     sc => sc.GetRequiredService<IOptions<SqsPollerConfig>>().Value,
                    //     sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), Queue),
                    //     new [] {typeof(FirstTestConsumer), typeof(SecondTestConsumer)}
                    // );
                })
                .UseConsoleLifetime();
            
            await hostBuilder.RunConsoleAsync();
        }
    }
}
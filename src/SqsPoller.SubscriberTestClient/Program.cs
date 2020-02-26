using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Extensions;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.SubscriberTestClient
{
    class Program
    {
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
                    var sqsSection = context.Configuration.GetSection("SQS");
                    services.Configure<SqsPollerConfig>(sqsSection);
                    services.AddSingleton<AwsAccountQueueUrlResolver>();
                    services.AddNamedSqsPollerConfig(PublisherTestClient.Program.FirstQueue, sqsSection);
                    services.AddNamedSqsPollerConfig(PublisherTestClient.Program.SecondQueue, sqsSection);

                    services.AddSqsPoller(
                        sc => sc.GetService<IOptionsSnapshot<SqsPollerConfig>>().Get(PublisherTestClient.Program.FirstQueue),
                        new [] {typeof(FirstTestConsumer)}
                    );
                    
                    services.AddSqsPoller(
                        sc => sc.GetService<IOptionsSnapshot<SqsPollerConfig>>().Get(PublisherTestClient.Program.SecondQueue),
                        new [] {typeof(SecondTestConsumer)}
                    );
                })
                .UseConsoleLifetime();

            await hostBuilder.RunConsoleAsync();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
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
                        sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), PublisherTestClient.Program.FirstQueue),
                        new [] {typeof(FirstTestConsumer)}
                    );
                    
                    services.AddSqsPoller(
                        sc => sc.GetRequiredService<IOptions<SqsPollerConfig>>().Value,
                        sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), PublisherTestClient.Program.SecondQueue),
                        new [] {typeof(SecondTestConsumer)}
                    );
                })
                .UseConsoleLifetime();
            
            var host = hostBuilder.UseConsoleLifetime().Build();
            var hostedServices = host.Services.GetRequiredService<IEnumerable<IHostedService>>();
            await host.RunAsync();
                
            //await hostBuilder.RunConsoleAsync();
        }
    }
}
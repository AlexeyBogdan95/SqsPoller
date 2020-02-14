using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqsPoller.Resolvers;

namespace SqsPoller.PublisherTestClient
{
    class Program
    {
        private const string Queue = "TestClient";

        static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: true);
            var configuration = builder.Build();
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            serviceCollection.Configure<SqsPollerConfig>(configuration.GetSection("SQS"));
            serviceCollection.AddSingleton<IAmazonSQS>(sc =>
            {
                var config = sc.GetRequiredService<IOptions<SqsPollerConfig>>();
                return SqsPollerConfiguration.CreateClient(config.Value);
            });
            serviceCollection.AddSingleton<IQueueUrlResolver>(sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), Queue));
            serviceCollection.AddSingleton(sc =>
            {
                var config = sc.GetRequiredService<IOptions<SqsPollerConfig>>();
                return new AmazonSqsService(config.Value, sc.GetRequiredService<IAmazonSQS>(),
                    sc.GetRequiredService<IQueueUrlResolver>());
            });
            
            var provider = serviceCollection.BuildServiceProvider();

            var publisher = provider.GetRequiredService<AmazonSqsService>();

            await publisher.CreateQueueAsync(Queue);

            Console.WriteLine("Publisher test client");
            Console.WriteLine("Please, press any button to send message or press ESCAPE to close application");

            ConsoleKeyInfo key = Console.ReadKey();

            while (key.Key != ConsoleKey.Escape)
            {
                var message = new Message
                {
                    Body = $"Test Message: {key.KeyChar.ToString()}",
                    Arguments = new Dictionary<string, object>()
                    {
                        {"First", 1},
                        {"Second", 2}
                    }
                };
                
                await publisher.PublishAsync(message);

                Console.WriteLine("Please, press any button to sen message or ESCAPE to close application");

                key = Console.ReadKey();
            }

            Environment.Exit(0);
        }
    }
}
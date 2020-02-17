using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Extensions;
using SqsPoller.Abstractions.Resolvers;
using SqsPoller.Publisher;

namespace SqsPoller.PublisherTestClient
{
    class Program
    {
        private const string Queue = "TestQueue";

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
                return config.Value.CreateClient();
            });
            serviceCollection.AddSingleton<IQueueUrlResolver>(sc => new AwsAccountQueueUrlResolver(sc.GetRequiredService<IAmazonSQS>(), Queue));
            serviceCollection.AddSingleton<AmazonSqsPublisher>();
            
            var provider = serviceCollection.BuildServiceProvider();

            var sqsClient = provider.GetRequiredService<IAmazonSQS>();
            await sqsClient.CreateQueueAsync(Queue);

            var sqsPublisher = provider.GetRequiredService<AmazonSqsPublisher>();

            Console.WriteLine("Publisher test client");
            Console.WriteLine("Please, press any button to send message or press ESCAPE to close application");

            ConsoleKeyInfo key = Console.ReadKey();

            while (key.Key != ConsoleKey.Escape)
            {
                if (char.IsLetter(key.KeyChar))
                {
                    await sqsPublisher.PublishAsync(new FirstTestMessage
                    {
                        FirstProperty = $"Test Message: {key.KeyChar.ToString()}",
                        Arguments = new Dictionary<string, object>()
                        {
                            {"First", 1},
                            {"Second", 2}
                        }
                    });
                }
                else
                {
                    await sqsPublisher.PublishAsync(new SecondTestMessage
                    {
                        SecondProperty = $"Test Message: {key.KeyChar.ToString()}",
                    });
                }

                Console.WriteLine("Please, press any button to send message or ESCAPE to close application");

                key = Console.ReadKey();
            }

            Environment.Exit(0);
        }
    }
}
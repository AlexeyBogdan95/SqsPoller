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
    public class Program
    {
        public const string FirstQueue = "FirstQueue";
        public const string SecondQueue = "SecondQueue";

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
            serviceCollection.AddTransient<AmazonSqsPublisher>();
            serviceCollection.AddSingleton<AwsAccountQueueUrlResolver>();
            
            var provider = serviceCollection.BuildServiceProvider();

            var sqsClient = provider.GetRequiredService<IAmazonSQS>();
            await sqsClient.CreateQueueAsync(FirstQueue);
            await sqsClient.CreateQueueAsync(SecondQueue);

            var queueUrlResolver = provider.GetRequiredService<AwsAccountQueueUrlResolver>();
            var sqsPublisher = provider.GetRequiredService<AmazonSqsPublisher>();

            Console.WriteLine("Publisher test client");
            Console.WriteLine("Please, press any button to send message or press ESCAPE to close application");

            ConsoleKeyInfo key = Console.ReadKey();

            while (key.Key != ConsoleKey.Escape)
            {
                if (char.IsLetter(key.KeyChar))
                {
                    var firstQueueUrl = await queueUrlResolver.Resolve(FirstQueue);
                    await sqsPublisher.PublishAsync(firstQueueUrl, new FirstTestMessage
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
                    var secondQueueUrl = await queueUrlResolver.Resolve(SecondQueue);
                    await sqsPublisher.PublishAsync(secondQueueUrl, new SecondTestMessage
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
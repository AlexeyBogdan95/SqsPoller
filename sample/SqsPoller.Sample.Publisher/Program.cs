using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqsPoller.Extensions.Publisher;

namespace SqsPoller.Sample.Publisher
{
    internal class Program
    {
        private static async Task Main()
        {
            var config = new AppConfig(
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .Build());
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IAmazonSQS>(provider => new AmazonSQSClient(
                config.AccessKey,
                config.SecretKey,
                new AmazonSQSConfig
                {
                    ServiceURL = config.ServiceUrl
                }));
            serviceCollection.AddSingleton<IAmazonSimpleNotificationService>(provider =>
                new AmazonSimpleNotificationServiceClient(
                    config.AccessKey,
                    config.SecretKey,
                    new AmazonSimpleNotificationServiceConfig
                    {
                        ServiceURL = config.ServiceUrl
                    }));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var sns = serviceProvider.GetService<IAmazonSimpleNotificationService>();
            var x = await sns.CreateTopicAsync(config.TopicName);
            var topicArn = x.TopicArn;

            var sqs = serviceProvider.GetService<IAmazonSQS>();
            await sqs.CreateQueueAsync(config.QueueName);
            var queueUrl = (await sqs.GetQueueUrlAsync(config.QueueName)).QueueUrl;

            await sns.SubscribeQueueAsync(topicArn, sqs, queueUrl);

            for (var i = 0; i < 10; i++)
            {
                var fooMessage = new FooMessage {Value = $"foo{i}"};
                await sns.PublishAsync(topicArn, fooMessage);
                Console.WriteLine($"The message {fooMessage.Value} has been published");

                var barMessage = new BarMessage {Value = $"bar{i}"};
                await sqs.SendMessageAsync(queueUrl, barMessage);
                Console.WriteLine($"The message {barMessage.Value} has been sent");
            }
        }
    }
}
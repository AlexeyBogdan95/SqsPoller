using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
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
            var sns = serviceProvider.GetRequiredService<IAmazonSimpleNotificationService>();
            var sqs = serviceProvider.GetRequiredService<IAmazonSQS>();
            
            for (var i = 0; i < 100; i++)
            {
                var fooMessage = new FooMessage {Value = $"foo{i}"};
                sns.PublishAsync(config.TopicArn, fooMessage);
                Console.WriteLine($"The message {fooMessage.Value} has been published");

                var barMessage = new BarMessage {Value = $"bar{i}"};
                sqs.SendMessageAsync(config.QueueUrl, barMessage);
                Console.WriteLine($"The message {barMessage.Value} has been sent");

                var cancelMessage = new CancelMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, cancelMessage);
                Console.WriteLine($"The message {cancelMessage.Value} has been sent");
                
                var mut1Message = new MutableOneMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, mut1Message);
                Console.WriteLine($"The message {mut1Message.Value} has been sent");
                
                var mut2Message = new MutableTwoMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, mut2Message);
                Console.WriteLine($"The message {mut2Message.Value} has been sent");
                
                var errMessage = new ErrorMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, errMessage);
                Console.WriteLine($"The message {errMessage.Value} has been sent");
                
                var httpMessage = new HttpMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, httpMessage);
                Console.WriteLine($"The message {httpMessage.Value} has been sent");
                
                var unkMessage = new UnknownMessage {Value = i};
                sqs.SendMessageAsync(config.QueueUrl, unkMessage);
                Console.WriteLine($"The message {unkMessage.Value} has been sent");
                
                var customRouteMessage = new CustomRouteMessage {Value = $"custom-route{i}"};
                sns.PublishAsync(new PublishRequest
                {
                    TopicArn = config.TopicArn,
                    Message = JsonSerializer.Serialize(customRouteMessage, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            "event", new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = "custom_route_message"
                            }
                        }
                    }
                });
                Console.WriteLine($"The message {customRouteMessage.Value} has been published");
            }

            var barMessageForSecondQueue = new BarMessage {Value = "barSecondQueue"};
            await sqs.SendMessageAsync(config.SecondQueueUrl, barMessageForSecondQueue);
            Console.WriteLine($"The message {barMessageForSecondQueue.Value} has been sent");
            
            var operationCancelledMessage = new OperationCancelledMessage();
            await sqs.SendMessageAsync(config.ThirdQueueUrl, operationCancelledMessage);
            Console.WriteLine($"The message {operationCancelledMessage.Value} has been sent");
        }
    }
}
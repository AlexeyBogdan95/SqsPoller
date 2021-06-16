using System;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SqsPoller
{
    public static class SqsPollerConfiguration
    {
        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services, SqsPollerConfig config, Type[] types)
        {
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();

            foreach (var type in types)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IConsumer), type));
            }
            
            services.AddTransient<IHostedService>(provider =>
            {
                AmazonSQSClient sqsClient;
                if (!string.IsNullOrEmpty(config.AccessKey) &&
                    !string.IsNullOrEmpty(config.SecretKey))
                {
                    sqsClient = new AmazonSQSClient(
                        config.AccessKey, config.SecretKey, CreateSqsConfig(config));
                }
                else
                {
                    sqsClient = new AmazonSQSClient(CreateSqsConfig(config));
                }

                return new SqsPollerHostedService(
                    sqsClient,
                    config,
                    provider.GetRequiredService<IConsumerResolver>(),
                    provider.GetRequiredService<ILogger<SqsPollerHostedService>>());
            });

            return services;
        }

        private static AmazonSQSConfig CreateSqsConfig(SqsPollerConfig config)
        {
            var amazonSqsConfig = new AmazonSQSConfig
            {
                ServiceURL = config.ServiceUrl
            };

            if (!string.IsNullOrEmpty(config.Region))
                amazonSqsConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region);
            
            return amazonSqsConfig;
        }
    }
}

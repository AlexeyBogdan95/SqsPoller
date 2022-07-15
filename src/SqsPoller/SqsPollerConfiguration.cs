using System;
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
            foreach (var type in types)
            {
                services.TryAdd(ServiceDescriptor.Singleton(type, type));
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

                var consumerResolver = new ConsumerResolver(provider, types, provider.GetRequiredService<ILogger<ConsumerResolver>>());
                return new SqsPollerHostedService(
                    sqsClient,
                    config,
                    consumerResolver,
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

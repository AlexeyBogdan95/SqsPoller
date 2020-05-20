using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SqsPoller
{
    public static class SqsPollerConfiguration
    {
        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services, SqsPollerConfig config, Assembly[] assembliesWithConsumers)
        {
            services.AddSingleton(config);
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();
            services.AddSingleton(provider =>
            {
                if (!string.IsNullOrEmpty(config.AccessKey) && 
                    !string.IsNullOrEmpty(config.SecretKey))
                {
                    return new AmazonSQSClient(
                        config.AccessKey, config.SecretKey,CreateSqsConfig(config));
                }

                return new AmazonSQSClient(CreateSqsConfig(config));
            });

            services.AddTransient<IHostedService, SqsPollerHostedService>();
            
            var types = assembliesWithConsumers
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && typeof(IConsumer).IsAssignableFrom(type))
                .ToArray();

            foreach (var type in types)
            {
                services.AddSingleton(typeof(IConsumer), type);
            }
            
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
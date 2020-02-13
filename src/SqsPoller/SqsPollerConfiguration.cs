using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;

namespace SqsPoller
{
    public static class SqsPollerConfiguration
    {
        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services,
            SqsPollerConfig config,
            params Assembly[] assembliesWithConsumers)
        {
            services.AddSqsPoller(config, new DefaultQueueUrlResolver(config), assembliesWithConsumers);
            return services;
        }

        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services,
            SqsPollerConfig config,
            IQueueUrlResolver queueUrlResolver,
            params Assembly[] assembliesWithConsumers)
        {
            services.AddSingleton(config);
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();
            services.AddSingleton<IQueueUrlResolver>(queueUrlResolver);
            services.AddSingleton<AmazonSQSClient>(sc => CreateClient(config));
            services.AddSingleton<AmazonSqsService>();
            services.AddHostedService<SqsPollerHostedService>();

            var types = assembliesWithConsumers.SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && typeof(IConsumer).IsAssignableFrom(x))
                .ToArray();

            foreach (var type in types)
            {
                services.AddSingleton(typeof(IConsumer), type);
            }

            return services;
        }

        private static AmazonSQSClient CreateClient(SqsPollerConfig config)
        {
            var amazonSqsConfig = new AmazonSQSConfig()
            {
                ServiceURL = config.ServiceUrl,
            };

            if (!string.IsNullOrEmpty(config.Region))
            {
                amazonSqsConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region);
            }

            return string.IsNullOrEmpty(config.AccessKey) || string.IsNullOrEmpty(config.SecretKey)
                ? new AmazonSQSClient(amazonSqsConfig)
                : new AmazonSQSClient(config.AccessKey, config.SecretKey, amazonSqsConfig);
        }
    }
}
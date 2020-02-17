using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqsPoller.Resolvers;

namespace SqsPoller
{
    public static class SqsPollerConfiguration
    {
        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services,
            SqsPollerConfig config,
            params Assembly[] assembliesWithConsumers)
        {
            var types = assembliesWithConsumers.SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && typeof(IConsumer).IsAssignableFrom(x))
                .ToArray();
            
            services.AddSqsPoller(sc => config, sc => new DefaultQueueUrlResolver(config), types);
            return services;
        }

        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services,
            Func<IServiceProvider, SqsPollerConfig> configFactory,
            Func<IServiceProvider, IQueueUrlResolver> queueUrlResolverFactory,
            Type[] consumerTypes)
        {
            services.AddSingleton<SqsPollerConfig>(configFactory);
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();
            services.AddSingleton<IQueueUrlResolver>(queueUrlResolverFactory);
            services.AddSingleton<IAmazonSQS>(sc => CreateClient(sc.GetRequiredService<IOptions<SqsPollerConfig>>().Value));
            services.AddSingleton<AmazonSqsService>();
            services.AddTransient<IHostedService, SqsPollerHostedService>(sc =>
            {
                var amazonSqsService = sc.GetRequiredService<AmazonSqsService>();
                var consumerResolver = new ConsumerResolver(sc.GetRequiredService<IEnumerable<IConsumer>>(), consumerTypes);
                var logger = sc.GetRequiredService<ILogger<SqsPollerHostedService>>();
                return new SqsPollerHostedService(amazonSqsService, consumerResolver, logger);
            });

            foreach (var type in consumerTypes)
            {
                services.AddSingleton(typeof(IConsumer), type);
            }

            return services;
        }

        internal static AmazonSQSClient CreateClient(SqsPollerConfig config)
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
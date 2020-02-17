using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Extensions;
using SqsPoller.Abstractions.Resolvers;
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
            
            services.AddSqsPoller(sc => config, sc => new DefaultQueueUrlResolver(Options.Create(config)), types);
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
            services.AddSingleton<IAmazonSQS>(sc =>
            {
                var config = sc.GetRequiredService<IOptions<SqsPollerConfig>>();
                return config.Value.CreateClient();
            });
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
    }
}
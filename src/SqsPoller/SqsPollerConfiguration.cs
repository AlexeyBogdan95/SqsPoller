using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqsPoller.Abstractions;
using SqsPoller.Abstractions.Extensions;
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
            
            services.AddSqsPoller(sc => config, types);
            return services;
        }

        public static IServiceCollection AddSqsPoller(
            this IServiceCollection services,
            Func<IServiceProvider, SqsPollerConfig> configFactory,
            Type[] consumerTypes)
        {
            services.TryAddSingleton<SqsPollerConfig>(configFactory);
            services.TryAddSingleton<IConsumerResolver, ConsumerResolver>();
            services.TryAddSingleton<IAmazonSQS>(sc =>
            {
                var config = sc.GetRequiredService<IOptions<SqsPollerConfig>>();
                return config.Value.CreateClient();
            });
            services.AddTransient<IHostedService, SqsPollerHostedService>(sc =>
            {
                var config = configFactory(sc);
                var amazonSqsReciever = new AmazonSqsReciever(config, sc.GetRequiredService<IAmazonSQS>());
                var consumerResolver = new ConsumerResolver(sc.GetRequiredService<IEnumerable<IConsumer>>(), consumerTypes);
                var logger = sc.GetRequiredService<ILogger<SqsPollerHostedService>>();
                return new SqsPollerHostedService(amazonSqsReciever, consumerResolver, logger);
            });

            foreach (var type in consumerTypes)
            {
                services.AddSingleton(typeof(IConsumer), type);
            }

            return services;
        }

        public static IServiceCollection AddNamedSqsPoller(
            this IServiceCollection services,
            string queueName,
            IConfigurationSection sqsSection,
            Type[] consumerTypes)
        {
            services.ConfigureNamedConfig(queueName, sqsSection);

            services.AddSqsPoller(
                sc => sc.GetService<IOptionsSnapshot<SqsPollerConfig>>().Get(queueName),
                consumerTypes
            );

            return services;
        }
    }
}
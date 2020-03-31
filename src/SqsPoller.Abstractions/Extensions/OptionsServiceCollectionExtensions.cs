using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.Abstractions.Extensions
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureNamedConfig(this IServiceCollection services, string name, IConfigurationSection sqsSection)
        {
            services.AddSingleton<AwsAccountQueueUrlResolver>();

            services
                .AddOptions<SqsPollerConfig>(name)
                // Don't use AwsAccountQueueUrlResolver because of circullar dependencies
                .Configure<IServiceProvider>((config, provider) =>
                {
                    sqsSection.Bind(config);
                    config.QueueUrl = provider.GetRequiredService<AwsAccountQueueUrlResolver>().Resolve(name).GetAwaiter().GetResult();
                });
            
            return services;
        }
    }
}
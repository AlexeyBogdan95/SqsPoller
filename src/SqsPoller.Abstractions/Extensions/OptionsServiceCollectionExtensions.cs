using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqsPoller.Abstractions.Resolvers;

namespace SqsPoller.Abstractions.Extensions
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddNamedSqsPollerConfig(this IServiceCollection services, string name, IConfigurationSection sqsSection)
        {
            services.AddSingleton<AwsAccountQueueUrlResolver>();

            services.AddOptions<SqsPollerConfig>(name).Configure<IServiceProvider>((config, sc) =>
            {
                sqsSection.Bind(config);
                var resolver = sc.GetRequiredService<AwsAccountQueueUrlResolver>();
                config.QueueUrl = resolver.Resolve(name).GetAwaiter().GetResult();
            });
            
            return services;
        }
    }
}
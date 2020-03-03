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
                .Configure<AwsAccountQueueUrlResolver>((config, resolver) =>
                {
                    sqsSection.Bind(config);
                    config.QueueUrl = resolver.Resolve(name).GetAwaiter().GetResult();
                });
            
            return services;
        }
    }
}
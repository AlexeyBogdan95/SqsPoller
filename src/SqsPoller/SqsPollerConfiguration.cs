using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SqsPoller
{
    public static class SqsPoolerConfiguration
    {
        public static IServiceCollection AddSqsPooler(
            this IServiceCollection services, SqsPoolerConfig config, Assembly[] assembliesWithConsumers)
        {
            services.AddSingleton(config);
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();
            services.AddSingleton(x => new AmazonSQSClient(new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region)
            }));

            services.AddTransient<IHostedService, SqsPoolerHostedService>();
            
            var types = assembliesWithConsumers.SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && typeof(IConsumer).IsAssignableFrom(x))
                .ToArray();

            foreach (var type in types)
            {
                services.AddSingleton(typeof(IConsumer), type);
            }
            
            return services;
        }
    }
}
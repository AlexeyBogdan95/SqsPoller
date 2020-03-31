using Amazon;
using Amazon.SQS;

namespace SqsPoller.Abstractions.Extensions
{
    public static class SqsPollerConfigExtensions
    {
        public static AmazonSQSClient CreateClient(this SqsPollerConfig config)
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
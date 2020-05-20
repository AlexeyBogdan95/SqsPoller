using Microsoft.Extensions.Configuration;

namespace SqsPoller.Sample.Publisher
{
    internal class AppConfig
    {
        internal AppConfig(IConfiguration configuration)
        {
            configuration.Bind(this);
        }

        public string ServiceUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
    }
}
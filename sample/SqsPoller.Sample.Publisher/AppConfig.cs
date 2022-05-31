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
        public string QueueUrl { get; set; } = string.Empty;
        public string SecondQueueUrl { get; set; } = string.Empty;
        public string ThirdQueueUrl { get; set; } = string.Empty;
        public string TopicArn { get; set; } = string.Empty;
    }
}
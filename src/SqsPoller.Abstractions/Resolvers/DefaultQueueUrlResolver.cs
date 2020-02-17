using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SqsPoller.Abstractions.Resolvers
{
    public class DefaultQueueUrlResolver : IQueueUrlResolver
    {
        private readonly SqsPollerConfig _sqsPollerConfig;

        public DefaultQueueUrlResolver(IOptions<SqsPollerConfig> sqsPollerConfig)
        {
            _sqsPollerConfig = sqsPollerConfig.Value;
        }
        
        public Task<string> Resolve(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sqsPollerConfig.QueueUrl);
        }
    }
}
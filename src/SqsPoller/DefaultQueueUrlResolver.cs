using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller
{
    public class DefaultQueueUrlResolver : IQueueUrlResolver
    {
        private readonly SqsPollerConfig _sqsPollerConfig;

        public DefaultQueueUrlResolver(SqsPollerConfig sqsPollerConfig)
        {
            _sqsPollerConfig = sqsPollerConfig;
        }
        
        public Task<string> Resolve(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_sqsPollerConfig.QueueUrl);
        }
    }
}
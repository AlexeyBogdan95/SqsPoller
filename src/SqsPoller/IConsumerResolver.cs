using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace SqsPoller
{
    public interface IConsumerResolver
    {
        Task Resolve(Message message, CancellationToken cancellationToken);
    }
}
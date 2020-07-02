using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller
{
    public interface IConsumerResolver
    {
        Task Resolve(string message, string messageType, CancellationToken cancellationToken);
    }
}
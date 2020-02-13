using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller
{
    public interface IQueueUrlResolver
    {
        Task<string> Resolve(CancellationToken cancellationToken = default);
    }
}
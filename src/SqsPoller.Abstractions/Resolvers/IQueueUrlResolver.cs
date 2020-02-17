using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Abstractions.Resolvers
{
    public interface IQueueUrlResolver
    {
        Task<string> Resolve(CancellationToken cancellationToken = default);
    }
}
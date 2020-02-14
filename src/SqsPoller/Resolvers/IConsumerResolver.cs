using System.Threading;

namespace SqsPoller.Resolvers
{
    internal interface IConsumerResolver
    {
        void Resolve(string message, string messageType, CancellationToken cancellationToken = default);
    }
}
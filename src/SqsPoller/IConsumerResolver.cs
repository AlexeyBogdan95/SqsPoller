using System.Threading;

namespace SqsPoller
{
    internal interface IConsumerResolver
    {
        void Resolve(string message, string messageType, CancellationToken cancellationToken);
    }
}
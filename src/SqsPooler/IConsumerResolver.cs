using System.Threading;

namespace SqsPooler
{
    internal interface IConsumerResolver
    {
        void Resolve(string message, string messageType, CancellationToken cancellationToken);
    }
}
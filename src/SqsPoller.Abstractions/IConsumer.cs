using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Abstractions
{
    /// <summary>
    /// It's used for IoC purposes only. Please don't use it
    /// </summary>
    /// <remarks>
    /// Please don't use it
    /// </remarks>
    public interface IConsumer
    {
    }

    public interface IConsumer<in T> : IConsumer
    {
        Task Consume(T message, CancellationToken cancellationToken = default);
    }
}
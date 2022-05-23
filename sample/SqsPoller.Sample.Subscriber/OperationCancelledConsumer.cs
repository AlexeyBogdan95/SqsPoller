using System;
using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Sample.Subscriber;

public class OperationCancelledMessage
{
    public int Value { get; set; }
}

public class OperationCancelledConsumer: IConsumer<OperationCancelledMessage>
{
    public Task Consume(OperationCancelledMessage message, CancellationToken cancellationToken)
    {
        throw new OperationCanceledException(cancellationToken);
    }
}
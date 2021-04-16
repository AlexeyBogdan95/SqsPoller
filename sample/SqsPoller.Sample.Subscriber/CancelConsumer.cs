using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Sample.Subscriber
{
    public class CancelMessage
    {
        public int Value { get; set; }
    }
    
    public class CancelConsumer: IConsumer<CancelMessage>
    {
        public Task Consume(CancelMessage message, CancellationToken cancellationToken)
        {
            return Task.Delay(1000, new CancellationToken(true));
        }
    }
}
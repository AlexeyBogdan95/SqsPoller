using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Sample.Subscriber
{
    public class MutableOneMessage
    {
        public int Value { get; set; }
    }
    
    public class MutableTwoMessage
    {
        public int Value { get; set; }
    }
    
    public class MutableConsumer: IConsumer<MutableOneMessage>, IConsumer<MutableTwoMessage>
    {
        private int _value = 1;
        
        public Task Consume(MutableOneMessage message, CancellationToken cancellationToken)
        {
            _value = message.Value % 2;
            return Task.CompletedTask;
        }

        public Task Consume(MutableTwoMessage message, CancellationToken cancellationToken)
        {
            var y = 6 / _value;
            return Task.CompletedTask;
        }
    }
}
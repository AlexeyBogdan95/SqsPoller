using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Sample.Subscriber
{
    public class ErrorMessage
    {
        public int Value { get; set; }
    }
    
    public class ErrorConsumer: IConsumer<ErrorMessage>
    {
        public Task Consume(ErrorMessage message, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
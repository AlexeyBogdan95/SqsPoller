using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class SecondMessageConsumer : IConsumer<SecondMessage>
    {
        private readonly IFakeService _fakeService;

        public SecondMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }
        
        public Task Consume(SecondMessage message, CancellationToken cancellationToken)
        {
            _fakeService.SecondMethod();
            return Task.CompletedTask;
        }
    }
}
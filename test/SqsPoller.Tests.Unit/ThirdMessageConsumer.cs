using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class ThirdMessageConsumer: IConsumer<FirstMessage>, IConsumer<SecondMessage>
    {
        private readonly IFakeService _fakeService;

        public ThirdMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public Task Consume(FirstMessage message, CancellationToken cancellationToken)
        {
            _fakeService.FirstMethod();
            return Task.CompletedTask;
        }

        public Task Consume(SecondMessage message, CancellationToken cancellationToken)
        {
            _fakeService.SecondMethod();
            return Task.CompletedTask;
        }
    }
}
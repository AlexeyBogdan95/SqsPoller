using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class FirstMessageConsumer : IConsumer<FirstMessage>
    {
        private readonly IFakeService _fakeService;

        public FirstMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public Task Consume(FirstMessage message, CancellationToken cancellationToken)
        {
            _fakeService.FirstMethod();
            return Task.CompletedTask;
        }
    }
}
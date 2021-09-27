using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class FirstCompressedMessageConsumer : IConsumer<FirstCompressedMessage>
    {
        private readonly IFakeService _fakeService;

        public FirstCompressedMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public Task Consume(FirstCompressedMessage message, CancellationToken cancellationToken)
        {
            _fakeService.FirstMethod(message);
            return Task.CompletedTask;
        }
    }
}

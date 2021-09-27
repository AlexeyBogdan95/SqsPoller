using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class SecondCompressedMessageConsumer : IConsumer<SecondCompressedMessage>
    {
        private readonly IFakeService _fakeService;

        public SecondCompressedMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public Task Consume(SecondCompressedMessage message, CancellationToken cancellationToken)
        {
            _fakeService.SecondMethod(message);
            return Task.CompletedTask;
        }
    }
}

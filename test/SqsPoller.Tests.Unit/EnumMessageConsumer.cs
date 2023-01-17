using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Tests.Unit
{
    public class EnumMessageConsumer : IConsumer<EnumMessage>
    {
        private readonly IFakeService _fakeService;

        public EnumMessageConsumer(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }
        
        public Task Consume(EnumMessage message, CancellationToken cancellationToken)
        {
            _fakeService.EnumMethod(message.Value);
            return Task.CompletedTask;
        }
    }
}
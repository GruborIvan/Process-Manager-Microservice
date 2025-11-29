using System.Threading;
using System.Threading.Tasks;
using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class SendEventsCommandHandlerTests
    {
        private readonly Mock<IOutboxService> _mockOutboxService = new Mock<IOutboxService>();

        [Fact]
        public async Task SendEventsCommand_Succeeds()
        {
            var command = new SendEventsCommand();

            var sendEventsCommandHandler = new SendEventsCommandHandler(_mockOutboxService.Object);
            await sendEventsCommandHandler.Handle(command, It.IsAny<CancellationToken>());

            _mockOutboxService.Verify(x => x.SendEventsAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}

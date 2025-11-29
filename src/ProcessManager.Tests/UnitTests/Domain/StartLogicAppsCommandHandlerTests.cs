using System.Threading;
using System.Threading.Tasks;
using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class StartLogicAppsCommandHandlerTests
    {
        private readonly Mock<IOutboxService> _mockOutboxService = new Mock<IOutboxService>();

        [Fact]
        public async Task StartLogicAppsCommand_Succeeds()
        {
            var command = new StartLogicAppsCommand();
            var guid = command.CommandId;

            var startLogicAppsCommandHandler = new StartLogicAppsCommandHandler(_mockOutboxService.Object);
            await startLogicAppsCommandHandler.Handle(command, It.IsAny<CancellationToken>());

            _mockOutboxService.Verify(x => x.StartLogicAppsAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}

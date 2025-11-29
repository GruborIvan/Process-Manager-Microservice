using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Validators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class CreateReportCommandHandlerTests
    {
        private readonly Mock<CreateReportCommandValidator> _mockValidator = new Mock<CreateReportCommandValidator>();
        private readonly Mock<IReportingService> _mockReportingService = new Mock<IReportingService>();

        public CreateReportCommandHandlerTests()
        {
            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<CreateReportCommand>()))
                .Verifiable();

            _mockReportingService
                .Setup(m => m.GetReportingDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, byte[]>());
        }

        [Fact]
        public async Task ValidCommand_CreateReport_Succeeds()
        {
            var command = new CreateReportCommand(
                correlationId: Guid.NewGuid(),
                dboEntities: new List<string> { "Activity", "WorkflowRun" },
                fromDatetime: DateTime.UtcNow,
                toDatetime: null);

            var createReportCommandHandler = new CreateReportCommandHandler(_mockReportingService.Object, _mockValidator.Object);
            await createReportCommandHandler.Handle(command, It.IsAny<CancellationToken>());

            _mockReportingService.Verify(x => x.GetReportingDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockReportingService.Verify(x => x.StoreReportAsync(It.IsAny<Guid>(), It.IsAny<Dictionary<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockValidator.Verify(x => x.ValidateAndThrow(It.IsAny<CreateReportCommand>()), Times.Once());
        }

        [Fact]
        public async Task NullCommand_Throws_ArgumentNullException()
        {
            // Arrange
            CreateReportCommand invalidCommand = null;

            _mockValidator.Setup(v => v.ValidateAndThrow(It.IsAny<CreateReportCommand>()))
                .Throws(new ArgumentNullException());

            var handler = new CreateReportCommandHandler(_mockReportingService.Object, _mockValidator.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(invalidCommand, default));

            _mockValidator.Verify();
        }
    }
}

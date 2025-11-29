using Moq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class DeleteOldOutboxMessagesCommandTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork = new Mock<IUnitOfWork>();
        private readonly int _olderThanXDays = 5;

        [Fact]
        public async Task DeleteOldOutboxMessagesCommand_Succeeds()
        {
            await Task.Run(() =>
            {
                // Arrange
                var mockUnitOfWork = new Mock<IUnitOfWork>();
                mockUnitOfWork.Setup(
                        repo => repo.OutboxRepository.DeleteRangeOlderThanAsync(_olderThanXDays, default))
                    .Verifiable();
                mockUnitOfWork.Setup(
                        repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .Verifiable();

                var command = new DeleteOldOutboxMessagesCommand(_olderThanXDays);
                var handler = new DeleteOldOutboxMessagesCommandHandler(mockUnitOfWork.Object);

                // Act
                Func<Task> testCode = async () =>
                {
                    await handler.Handle(command, new CancellationToken());
                };

                var actionResult = Record.ExceptionAsync(testCode);

                // Assert
                Assert.Null(actionResult.Exception);
                Assert.Equal("RanToCompletion", actionResult.Status.ToString());

                mockUnitOfWork.Verify();
            });
        }
    }
}

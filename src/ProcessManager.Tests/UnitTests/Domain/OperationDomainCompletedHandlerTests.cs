using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using ProcessManager.Domain.DomainEvents;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class OperationDomainCompletedHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;
        private readonly Mock<IMapper> _mapperMock;

        public OperationDomainCompletedHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.Setup(
                    repo => repo.OutboxRepository.AddAsync(It.IsAny<Guid>(), It.IsAny<OutboxMessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OutboxMessage())
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Verifiable();

            _contextAccessorMock = new Mock<IContextAccessor>();
            _contextAccessorMock.Setup(x => x.GetCommandId())
                .Returns(Guid.NewGuid)
                .Verifiable();

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(x => x.Map<ProcessFailed>(It.IsAny<OperationDomainCompleted>()))
                .Returns(new ProcessFailed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    new ErrorData("error message", "", "process"), "test/test"));
            _mapperMock.Setup(x => x.Map<ProcessSucceeded>(It.IsAny<OperationDomainCompleted>()))
                .Returns(new ProcessSucceeded(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test/test"));
        }

        [Fact]
        public async Task OperationDomainCompleted_ProcessFailed_SaveToOutbox_Succeeds()
        {
            // Arrange
            var notification = new OperationDomainCompleted(Guid.NewGuid(), new ErrorData("error message", "", "process"), "test/test", "failed");
            var handler = new OperationDomainCompletedHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<ProcessFailed>(It.IsAny<OperationDomainCompleted>()), Times.Once);
            _mapperMock.Verify(x => x.Map<ProcessSucceeded>(It.IsAny<OperationDomainCompleted>()), Times.Never);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OperationDomainCompleted_ProcessSucceeded_SaveToOutbox_Succeeds()
        {
            // Arrange
            var notification = new OperationDomainCompleted(Guid.NewGuid(), null, "test/test", "succeeded");
            var handler = new OperationDomainCompletedHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<ProcessFailed>(It.IsAny<OperationDomainCompleted>()), Times.Never);
            _mapperMock.Verify(x => x.Map<ProcessSucceeded>(It.IsAny<OperationDomainCompleted>()), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

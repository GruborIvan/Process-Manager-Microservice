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
    public class OperationDomainFailedHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;
        private readonly Mock<IMapper> _mapperMock;

        public OperationDomainFailedHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.Setup(
                    repo => repo.OutboxRepository.AddAsync(It.IsAny<Guid>(), It.IsAny<OutboxMessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OutboxMessage())
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.WorkflowRepository.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowRun())
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.WorkflowRepository.Update(It.IsAny<WorkflowRun>()))
                .Verifiable();

            _unitOfWorkMock.Setup(
                    repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Verifiable();

            _contextAccessorMock = new Mock<IContextAccessor>();
            _contextAccessorMock.Setup(x => x.GetCommandId())
                .Returns(Guid.NewGuid)
                .Verifiable();

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(x => x.Map<ProcessFailed>(It.IsAny<OperationDomainFailed>()))
                .Returns(new ProcessFailed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    new ErrorData("error message", "", "process"), "test/test"));
        }

        [Fact]
        public async Task OperationDomainFailed_SaveToOutbox_Succeeds()
        {
            // Arrange
            var operationId = Guid.NewGuid();
            var notification = new OperationDomainFailed(operationId, new ErrorData("error message", "", "process"), "test/test");
            var handler = new OperationDomainFailedHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<ProcessFailed>(It.IsAny<OperationDomainFailed>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/") && x.Contains("ProcessManager.Domain.Events.ProcessFailed")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.GetAsync(It.Is<Guid>(x => x == operationId), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.Update(It.IsAny<WorkflowRun>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

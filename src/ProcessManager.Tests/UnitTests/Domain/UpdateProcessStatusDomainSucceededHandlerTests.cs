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
    public class UpdateProcessStatusDomainSucceededHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;
        private readonly Mock<IMapper> _mapperMock;

        public UpdateProcessStatusDomainSucceededHandlerTests()
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
            _mapperMock.Setup(x => x.Map<UpdateProcessStatusSucceeded>(It.IsAny<UpdateProcessStatusDomainSucceeded>()))
                .Returns(new UpdateProcessStatusSucceeded(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task UpdateProcessStatusDomainSucceeded_SaveToOutbox_Succeeds()
        {
            // Arrange
            var workflow = new WorkflowRun
            (
                Guid.NewGuid(),
                "Name",
                "in progress",
                Guid.NewGuid().ToString(),
                null
            );
            var notification = new UpdateProcessStatusDomainSucceeded(workflow);
            var handler = new UpdateProcessStatusDomainSucceededHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<UpdateProcessStatusSucceeded>(It.IsAny<UpdateProcessStatusDomainSucceeded>()), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

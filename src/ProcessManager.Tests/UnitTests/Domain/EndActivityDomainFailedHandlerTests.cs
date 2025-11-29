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
    public class EndActivityDomainFailedHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;
        private readonly Mock<IMapper> _mapperMock;

        public EndActivityDomainFailedHandlerTests()
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
            _mapperMock.Setup(x => x.Map<EndActivityFailed>(It.IsAny<EndActivityDomainFailed>()))
                .Returns(new EndActivityFailed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    "completed", DateTime.Now, "test/test", new ErrorData("error message", "", "process")));
        }

        [Fact]
        public async Task EndActivityDomainFailed_SaveToOutbox_Succeeds()
        {
            // Arrange
            var notification = new EndActivityDomainFailed(Guid.NewGuid(), Guid.NewGuid(), "completed", DateTime.Now, "test/test", new ErrorData("error message", "", "process"));
            var handler = new EndActivityDomainFailedHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<EndActivityFailed>(It.IsAny<EndActivityDomainFailed>()), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Activities/")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

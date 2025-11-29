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
    public class EndActivityDomainSucceededHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IContextAccessor> _contextAccessorMock;
        private readonly Mock<IMapper> _mapperMock;

        public EndActivityDomainSucceededHandlerTests()
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
            _mapperMock.Setup(x => x.Map<EndActivitySucceeded>(It.IsAny<EndActivityDomainSucceeded>()))
                .Returns(new EndActivitySucceeded(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    "test", "completed", DateTime.Now, "test/test"));
        }

        [Fact]
        public async Task EndActivityDomainSucceeded_SaveToOutbox_Succeeds()
        {
            // Arrange
            var activity = new Activity(
                Guid.NewGuid(), 
                Guid.NewGuid(),
                "test",
                "in progress",
                "test/test",
                DateTime.Now);
            var notification = new EndActivityDomainSucceeded(activity);
            var handler = new EndActivityDomainSucceededHandler(_unitOfWorkMock.Object, _contextAccessorMock.Object, _mapperMock.Object);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _mapperMock.Verify(x => x.Map<EndActivitySucceeded>(It.IsAny<EndActivityDomainSucceeded>()), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Activities/")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

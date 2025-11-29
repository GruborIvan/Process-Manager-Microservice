using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using FiveDegrees.Messages.ProcessManager.v2;
using ProcessManager.Domain.Models;
using Rebus.Bus;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class UpdateActivityMessageHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<UpdateActivityMessageHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessor;

        public UpdateActivityMessageHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Activity(Guid.NewGuid(), Guid.NewGuid(), "TestActivity", "in progress", $"api/Activities/{Guid.NewGuid()}", DateTime.Now))
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsg>()))
                .Returns(new UpdateActivityCommand(Guid.NewGuid(), "test", "test"))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsgV2>()))
                .Returns(new UpdateActivityCommand(Guid.NewGuid(), "test", "test"))
                .Verifiable();

            _loggerMock = new Mock<ILogger<UpdateActivityMessageHandler>>();
            _mockContextAccessor = new Mock<IContextAccessor>();
            _mockContextAccessor.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessor.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task UpdateActivity_UpdateActivityCommandIsSent()
        {
            var msg = new UpdateActivityMsg(Guid.NewGuid(), Guid.NewGuid(), "test", "test");

            var updateActivityMessageHandler = new UpdateActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await updateActivityMessageHandler.Handle(msg);

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task UpdateActivityV2_UpdateActivityCommandIsSent()
        {
            var msg = new UpdateActivityMsgV2(Guid.NewGuid(), "test", "test");

            var updateActivityMessageHandler = new UpdateActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await updateActivityMessageHandler.Handle(msg);

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task UpdateActivity_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var msg = new UpdateActivityMsg(Guid.NewGuid(), Guid.NewGuid(), "test", "test");

            var updateActivityMessageHandler = new UpdateActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => updateActivityMessageHandler.Handle(msg));

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task UpdateActivityV2_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var msg = new UpdateActivityMsgV2(Guid.NewGuid(), "test", "test");

            var updateActivityMessageHandler = new UpdateActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => updateActivityMessageHandler.Handle(msg));

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateActivityCommand>(It.IsAny<UpdateActivityMsgV2>()), Times.Once());
        }
    }
}

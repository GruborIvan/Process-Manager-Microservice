using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using FiveDegrees.Messages.ProcessManager.v2;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using Rebus.Bus;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class StartActivityMessageHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<StartActivityMessageHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessor;

        public StartActivityMessageHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Activity(Guid.NewGuid(), Guid.NewGuid(), "TestActivity", "in progress", $"api/Activities/{Guid.NewGuid()}", DateTime.Now))
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<StartActivityCommand>(It.IsAny<StartActivityMsg>()))
                .Returns(new StartActivityCommand(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now, ""))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<StartActivityCommand>(It.IsAny<StartActivityMsgV2>()))
                .Returns(new StartActivityCommand(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now, ""))
                .Verifiable();

            _loggerMock = new Mock<ILogger<StartActivityMessageHandler>>();

            _mockContextAccessor = new Mock<IContextAccessor>();
            _mockContextAccessor.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessor.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task StartActivityMsg_StartActivityCommandIsSent()
        {
            var startActivityMsg = new StartActivityMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var startActivityMessageHandler = new StartActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await startActivityMessageHandler.Handle(startActivityMsg);

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartActivityCommand>(It.IsAny<StartActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task StartActivityMsgV2_StartActivityCommandIsSent()
        {
            var startActivityMsg = new StartActivityMsgV2(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var startActivityMessageHandler = new StartActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await startActivityMessageHandler.Handle(startActivityMsg);

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartActivityCommand>(It.IsAny<StartActivityMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task StartActivityMsg_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startActivityMsg = new StartActivityMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var startActivityMessageHandler = new StartActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => startActivityMessageHandler.Handle(startActivityMsg));

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartActivityCommand>(It.IsAny<StartActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task StartActivityMsgV2_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startActivityMsg = new StartActivityMsgV2(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var startActivityMessageHandler = new StartActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => startActivityMessageHandler.Handle(startActivityMsg));

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartActivityCommand>(It.IsAny<StartActivityMsgV2>()), Times.Once());
        }
    }
}

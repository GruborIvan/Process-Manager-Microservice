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
    public class EndActivityMessageHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<EndActivityMessageHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessorMock;

        public EndActivityMessageHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Activity(Guid.NewGuid(), Guid.NewGuid(), "TestActivity", "in progress", $"api/Activities/{Guid.NewGuid()}", DateTime.Now))
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<EndActivityCommand>(It.IsAny<EndActivityMsg>()))
                .Returns(new EndActivityCommand(Guid.NewGuid(), "", DateTime.Now, null))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<EndActivityCommand>(It.IsAny<EndActivityMsgV2>()))
                .Returns(new EndActivityCommand(Guid.NewGuid(), "", DateTime.Now, null))
                .Verifiable();

            _loggerMock = new Mock<ILogger<EndActivityMessageHandler>>();

            _mockContextAccessorMock = new Mock<IContextAccessor>();
            _mockContextAccessorMock.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessorMock.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task EndActivityMsg_EndActivityCommandIsSent()
        {
            var endActivity = new EndActivityMsg(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var endActivityMessageHandler = new EndActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessorMock.Object, _busMock.Object);

            await endActivityMessageHandler.Handle(endActivity);

            _mediatorMock.Verify(x => x.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<EndActivityCommand>(It.IsAny<EndActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task EndActivityMsg_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var endActivity = new EndActivityMsg(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var endActivityMessageHandler = new EndActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessorMock.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => endActivityMessageHandler.Handle(endActivity));

            _mediatorMock.Verify(x => x.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<EndActivityCommand>(It.IsAny<EndActivityMsg>()), Times.Once());
        }

        [Fact]
        public async Task EndActivityMsgV2_EndActivityCommandIsSent()
        {
            var endActivity = new EndActivityMsgV2(Guid.NewGuid(), "", DateTime.Now);

            var endActivityMessageHandler = new EndActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessorMock.Object, _busMock.Object);

            await endActivityMessageHandler.Handle(endActivity);

            _mediatorMock.Verify(x => x.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<EndActivityCommand>(It.IsAny<EndActivityMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task EndActivityMsgV2_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var endActivity = new EndActivityMsgV2(Guid.NewGuid(), "", DateTime.Now);

            var endActivityMessageHandler = new EndActivityMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessorMock.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => endActivityMessageHandler.Handle(endActivity));

            _mediatorMock.Verify(x => x.Send(It.IsAny<EndActivityCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<EndActivityCommand>(It.IsAny<EndActivityMsgV2>()), Times.Once());
        }
    }
}

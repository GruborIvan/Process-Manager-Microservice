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
using Rebus.Bus;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class UpdateProcessStatusMessageHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<UpdateProcessStatusMessageHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessor;

        public UpdateProcessStatusMessageHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit())
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsg>()))
                .Returns(new UpdateProcessStatusCommand(Guid.NewGuid(), "", DateTime.Now, null, null))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsgV2>()))
                .Returns(new UpdateProcessStatusCommand(Guid.NewGuid(), "", DateTime.Now, null, null))
                .Verifiable();

            _loggerMock = new Mock<ILogger<UpdateProcessStatusMessageHandler>>();

            _mockContextAccessor = new Mock<IContextAccessor>();
            _mockContextAccessor.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessor.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task UpdateProcessStatus_UpdateProcessStatusCommandIsSent()
        {
            var updateProcessStatus = new UpdateProcessStatusMsg(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var updateProcessStatusMessageHandler = new UpdateProcessStatusMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await updateProcessStatusMessageHandler.Handle(updateProcessStatus);

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsg>()), Times.Once());
        }

        [Fact]
        public async Task UpdateProcessStatusV2_UpdateProcessStatusCommandIsSent()
        {
            var updateProcessStatus = new UpdateProcessStatusMsgV2(Guid.NewGuid(), "", DateTime.Now);

            var updateProcessStatusMessageHandler = new UpdateProcessStatusMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await updateProcessStatusMessageHandler.Handle(updateProcessStatus);

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task UpdateProcessStatus_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var updateProcessStatus = new UpdateProcessStatusMsg(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.Now);

            var updateProcessStatusMessageHandler = new UpdateProcessStatusMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => updateProcessStatusMessageHandler.Handle(updateProcessStatus));

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsg>()), Times.Once());
        }

        [Fact]
        public async Task UpdateProcessStatusV2_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var updateProcessStatus = new UpdateProcessStatusMsgV2(Guid.NewGuid(), "", DateTime.Now);

            var updateProcessStatusMessageHandler = new UpdateProcessStatusMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => updateProcessStatusMessageHandler.Handle(updateProcessStatus));

            _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateProcessStatusCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<UpdateProcessStatusCommand>(It.IsAny<UpdateProcessStatusMsgV2>()), Times.Once());
        }
    }
}

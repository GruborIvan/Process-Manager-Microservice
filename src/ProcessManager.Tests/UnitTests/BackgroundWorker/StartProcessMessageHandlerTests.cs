using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FiveDegrees.Messages.EntityRelation;
using FiveDegrees.Messages.Orchestrator;
using FiveDegrees.Messages.Orchestrator.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Tests.UnitTests.Domain;
using Rebus.Bus;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class StartProcessMessageHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<StartProcessMessageHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessor;

        public StartProcessMessageHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit())
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<StartProcessCommand>(It.IsAny<IStartProcessMsg>()))
                .Returns(new StartProcessCommand(null, null, Guid.NewGuid(), null, Guid.NewGuid(), null, null))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV2>()))
                .Returns(new StartProcessCommand(null, null, Guid.NewGuid(), null, Guid.NewGuid(), null, null))
                .Verifiable();
            _autoMapperMock
                .Setup(m => m.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV3>()))
                .Returns(new StartProcessCommand(null, null, Guid.NewGuid(), null, Guid.NewGuid(), null, "sbx"))
                .Verifiable();

            _loggerMock = new Mock<ILogger<StartProcessMessageHandler>>();

            _mockContextAccessor = new Mock<IContextAccessor>();
            _mockContextAccessor.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessor.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task StartProcess_StartProcessCommandIsSent()
        {
            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartDeleteEntityRelationMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            await startProcessMessageHandler.Handle(message);
            
            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsg>()), Times.Once());
        }

        [Fact]
        public async Task StartProcessV2_StartProcessCommandIsSent()
        {
            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartCreateEnvironmentMsgV2(Guid.NewGuid(), "westeurope", Guid.NewGuid(), "Sandbox", "sbx", "tnt", null);

            await startProcessMessageHandler.Handle(message);

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task StartProcessV3_StartProcessCommandIsSent()
        {
            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartProcessMsgV3(Guid.NewGuid(), "sbx", null);

            await startProcessMessageHandler.Handle(message);

            _mediatorMock.Verify(x => x.Send(It.Is<StartProcessCommand>(x=> x.EnvironmentShortName == "sbx"), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV3>()), Times.Once());
        }

        [Fact]
        public async Task StartProcess_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartDeleteEntityRelationMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            await Assert.ThrowsAsync<Exception>(() => startProcessMessageHandler.Handle(message));

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsg>()), Times.Once());
        }

        [Fact]
        public async Task StartProcessV2_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartCreateEnvironmentMsgV2(Guid.NewGuid(), "westeurope", Guid.NewGuid(), "Sandbox", "sbx", "tnt", null);

            await Assert.ThrowsAsync<Exception>(() => startProcessMessageHandler.Handle(message));

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV2>()), Times.Once());
        }

        [Fact]
        public async Task StartProcessV3_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startProcessMessageHandler = new StartProcessMessageHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartProcessMsgV3(Guid.NewGuid(), "sbx", null);

            await Assert.ThrowsAsync<Exception>(() => startProcessMessageHandler.Handle(message));

            _mediatorMock.Verify(x => x.Send(It.Is<StartProcessCommand>(x => x.EnvironmentShortName == "sbx"), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<IStartProcessMsgV3>()), Times.Once());
        }
    }
}

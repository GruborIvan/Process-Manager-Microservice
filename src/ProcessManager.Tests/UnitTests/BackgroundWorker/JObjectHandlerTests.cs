using AutoMapper;
using FiveDegrees.Messages.EntityRelation;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using Rebus.Bus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class JObjectHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<JObjectHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;
        private readonly Mock<IContextAccessor> _mockContextAccessor;

        public JObjectHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit())
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<StartProcessCommand>(It.IsAny<JObject>()))
                .Returns(new StartProcessCommand(null, null, Guid.NewGuid(), null, Guid.NewGuid(), null, null))
                .Verifiable();

            _loggerMock = new Mock<ILogger<JObjectHandler>>();

            _mockContextAccessor = new Mock<IContextAccessor>();
            _mockContextAccessor.Setup(m => m.GetRequestId()).Returns(Guid.NewGuid());
            _mockContextAccessor.Setup(m => m.GetCommandId()).Returns(Guid.NewGuid());

            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task StartProcess_StartProcessCommandIsSent()
        {
            var startProcessMessageHandler = new JObjectHandler(_mediatorMock.Object, _autoMapperMock.Object, _loggerMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartDeleteEntityRelationMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var messageString = JsonConvert.SerializeObject(message);
            var jobj = JsonConvert.DeserializeObject<JObject>(messageString);

            await startProcessMessageHandler.Handle(jobj);

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<JObject>()), Times.Once());
        }

        [Fact]
        public async Task StartProcess_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var startProcessMessageHandler = new JObjectHandler(_mediatorMock.Object, _autoMapperMock.Object, _loggerMock.Object, _mockContextAccessor.Object, _busMock.Object);

            var message = new StartDeleteEntityRelationMsg(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var messageString = JsonConvert.SerializeObject(message);
            var jobj = JsonConvert.DeserializeObject<JObject>(messageString);

            await Assert.ThrowsAsync<Exception>(() => startProcessMessageHandler.Handle(jobj));

            _mediatorMock.Verify(x => x.Send(It.IsAny<StartProcessCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<StartProcessCommand>(It.IsAny<JObject>()), Times.Once());
        }
    }
}

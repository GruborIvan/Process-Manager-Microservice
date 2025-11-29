using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using Rebus.Bus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class InsertWorkflowRunMsgHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<InsertWorkflowRunMsgHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;

        public InsertWorkflowRunMsgHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<InsertWorkflowRunCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Unit())
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<InsertWorkflowRunCommand>(It.IsAny<InsertWorkflowRunMsg>()))
                .Returns(new InsertWorkflowRunCommand(Guid.NewGuid(), Guid.NewGuid(), "", "", new List<string>()))
                .Verifiable();

            _loggerMock = new Mock<ILogger<InsertWorkflowRunMsgHandler>>();

            _busMock = new Mock<IBus>();
        }
    
        [Fact]
        public async Task InsertWorkflowRun_InsertWorkflowRunCommandIsSent()
        {
            var insertWorkflowRunMessageHandler = new InsertWorkflowRunMsgHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _busMock.Object);
     
            var message = new InsertWorkflowRunMsg(Guid.NewGuid(), Guid.NewGuid().ToString(), "NewWorkflowName", "NewWorkflowId", null);
     
            await insertWorkflowRunMessageHandler.Handle(message);
            
            _mediatorMock.Verify(x => x.Send(It.IsAny<InsertWorkflowRunCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<InsertWorkflowRunCommand>(It.IsAny<InsertWorkflowRunMsg>()), Times.Once());
        }
     
        [Fact]
        public async Task InsertWorkflowRun_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<InsertWorkflowRunCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();
     
            var insertWorkflowRunMessageHandler = new InsertWorkflowRunMsgHandler(_mediatorMock.Object, _loggerMock.Object, _autoMapperMock.Object, _busMock.Object);
     
            var message = new InsertWorkflowRunMsg(Guid.Empty, "", "", "", null);
     
            await Assert.ThrowsAsync<Exception>(() => insertWorkflowRunMessageHandler.Handle(message));
     
            _mediatorMock.Verify(x => x.Send(It.IsAny<InsertWorkflowRunCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<InsertWorkflowRunCommand>(It.IsAny<InsertWorkflowRunMsg>()), Times.Once());
        }
    }
}

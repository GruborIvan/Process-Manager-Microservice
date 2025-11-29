using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.BackgroundWorker.Handlers;
using ProcessManager.Domain.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker
{
    public class ReportingProcessManagerHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _autoMapperMock;
        private readonly Mock<ILogger<ReportingProcessManagerHandler>> _loggerMock;
        private readonly Mock<IBus> _busMock;

        public ReportingProcessManagerHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            _autoMapperMock = new Mock<IMapper>();
            _autoMapperMock
                .Setup(m => m.Map<CreateReportCommand>(It.IsAny<ReportingProcessManagerMsg>()))
                .Returns(new CreateReportCommand(Guid.NewGuid(), new List<string> { "Activity" }, DateTime.Now, null))
                .Verifiable();

            _loggerMock = new Mock<ILogger<ReportingProcessManagerHandler>>();
            _busMock = new Mock<IBus>();
        }

        [Fact]
        public async Task ReportingProcessManager_CreateReportCommandIsSent()
        {
            var message = new ReportingProcessManagerMsg(Guid.NewGuid(), new List<ReportingProcessManagerEntities> { ReportingProcessManagerEntities.Activity }, DateTime.Now, null);

            var reportingProcessManagerHandler = new ReportingProcessManagerHandler(_loggerMock.Object, _mediatorMock.Object, _autoMapperMock.Object, _busMock.Object);

            await reportingProcessManagerHandler.Handle(message);

            _mediatorMock.Verify(x => x.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<CreateReportCommand>(It.IsAny<ReportingProcessManagerMsg>()), Times.Once());
        }

        [Fact]
        public async Task ReportingProcessManager_SendingMessageFailed_ThrowsException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception())
                .Verifiable();

            var message = new ReportingProcessManagerMsg(Guid.NewGuid(), new List<ReportingProcessManagerEntities> { ReportingProcessManagerEntities.Activity }, DateTime.Now, null);

            var reportingProcessManagerHandler = new ReportingProcessManagerHandler(_loggerMock.Object, _mediatorMock.Object, _autoMapperMock.Object, _busMock.Object);

            await Assert.ThrowsAsync<Exception>(() => reportingProcessManagerHandler.Handle(message));

            _mediatorMock.Verify(x => x.Send(It.IsAny<CreateReportCommand>(), It.IsAny<CancellationToken>()), Times.Once());
            _autoMapperMock.Verify(x => x.Map<CreateReportCommand>(It.IsAny<ReportingProcessManagerMsg>()), Times.Once());
        }
    }
}

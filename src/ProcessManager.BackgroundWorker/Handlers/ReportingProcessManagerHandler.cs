using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using MediatR;
using Microsoft.Extensions.Logging;
using ProcessManager.Domain.Commands;
using Rebus.Handlers;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Retry.Simple;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class ReportingProcessManagerHandler : IHandleMessages<ReportingProcessManagerMsg>, IHandleMessages<IFailed<ReportingProcessManagerMsg>>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IBus _bus;

        public ReportingProcessManagerHandler(
            ILogger<ReportingProcessManagerHandler> logger, 
            IMediator mediator, 
            IMapper mapper,
            IBus bus)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _bus = bus;
        }

        public async Task Handle(ReportingProcessManagerMsg message)
        {
            _logger.LogInformation($"Received {nameof(ReportingProcessManagerMsg)} with CorrelationId: {message.CorrelationId}");
            var command = _mapper.Map<CreateReportCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<ReportingProcessManagerMsg> message)
        {
            _logger.LogError($"{nameof(ReportingProcessManagerMsg)} failed with CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");
            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

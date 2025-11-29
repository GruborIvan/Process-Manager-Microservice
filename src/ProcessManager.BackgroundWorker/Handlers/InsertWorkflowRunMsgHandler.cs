using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using MediatR;
using Microsoft.Extensions.Logging;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.DomainEvents;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class InsertWorkflowRunMsgHandler : IHandleMessages<InsertWorkflowRunMsg>, IHandleMessages<IFailed<InsertWorkflowRunMsg>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IBus _bus;

        public InsertWorkflowRunMsgHandler(
            IMediator mediator, 
            ILogger<InsertWorkflowRunMsgHandler> logger,
            IMapper mapper,
            IBus bus)
        {
            _mediator = mediator;
            _logger = logger;
            _mapper = mapper;
            _bus = bus;
        }
        public async Task Handle(InsertWorkflowRunMsg message)
        {
            _logger.LogInformation($"Received {nameof(InsertWorkflowRunMsg)} with UnorchestratedRunId: {message.UnorchestratedRunId}, OperationId: {message.OperationId}, WorkflowRunId: {message.WorkflowRunId}");
            if (!Guid.TryParse(message.OperationId, out var newGuid)) message.OperationId = null;
            var command = _mapper.Map<InsertWorkflowRunCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<InsertWorkflowRunMsg> message)
        {
            _logger.LogError($"{nameof(InsertWorkflowRunMsg)} failed with UnorchestratedRunId: {message.Message.UnorchestratedRunId}, OperationId: {message.Message.OperationId}, WorkflowRunId: {message.Message.WorkflowRunId} and error description: {message.ErrorDescription}.");
            await _mediator.Publish(_mapper.Map<InsertWorkflowRunDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}
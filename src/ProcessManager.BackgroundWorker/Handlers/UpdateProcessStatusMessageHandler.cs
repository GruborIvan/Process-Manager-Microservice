using System.Linq;
using AutoMapper;
using FiveDegrees.Messages.ProcessManager;
using FiveDegrees.Messages.ProcessManager.v2;
using MediatR;
using Microsoft.Extensions.Logging;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Interfaces;
using Rebus.Handlers;
using System.Threading.Tasks;
using ProcessManager.Domain.DomainEvents;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Retry.Simple;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class UpdateProcessStatusMessageHandler : IHandleMessages<UpdateProcessStatusMsg>, IHandleMessages<UpdateProcessStatusMsgV2>,
        IHandleMessages<IFailed<UpdateProcessStatusMsg>>, IHandleMessages<IFailed<UpdateProcessStatusMsgV2>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public UpdateProcessStatusMessageHandler(
            IMediator mediator, 
            ILogger<UpdateProcessStatusMessageHandler> logger,
            IMapper mapper,
            IContextAccessor contextAccessor,
            IBus bus)
        {
            _mediator = mediator;
            _logger = logger;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _bus = bus;
        }

        public async Task Handle(UpdateProcessStatusMsg message)
        {
            _logger.LogInformation($"Received message: {nameof(UpdateProcessStatusMsg)} \n" +
                $" with {nameof(message.CorrelationId)}: {message.CorrelationId} \n" +
                $" and: {nameof(message.OperationId)}: {message.OperationId}.");

            var command = _mapper.Map<UpdateProcessStatusCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<UpdateProcessStatusMsg> message)
        {
            _logger.LogError($"{nameof(UpdateProcessStatusMsg)} failed with \n" +
                             $"CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<UpdateProcessStatusDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _mediator.Publish(new OperationDomainFailed(message.Message.OperationId, new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), message.Message.Resource));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task Handle(UpdateProcessStatusMsgV2 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"Received message: {nameof(UpdateProcessStatusMsgV2)} \n" +
                $" with RequestId: {_contextAccessor.GetRequestId()} \n" +
                $" and: {nameof(message.OperationId)}: {message.OperationId}.");

            var command = _mapper.Map<UpdateProcessStatusCommand>(message);

            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<UpdateProcessStatusMsgV2> message)
        {
            _logger.LogError($"{nameof(UpdateProcessStatusMsgV2)} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<UpdateProcessStatusDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _mediator.Publish(new OperationDomainFailed(message.Message.OperationId, new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), message.Message.Resource));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

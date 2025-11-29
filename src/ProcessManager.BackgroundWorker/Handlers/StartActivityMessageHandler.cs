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
using Rebus.Retry.Simple;
using Rebus.Exceptions;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class StartActivityMessageHandler : IHandleMessages<StartActivityMsg>, IHandleMessages<StartActivityMsgV2>,
        IHandleMessages<IFailed<StartActivityMsg>>, IHandleMessages<IFailed<StartActivityMsgV2>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public StartActivityMessageHandler(
            IMediator mediator, 
            ILogger<StartActivityMessageHandler> logger,
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

        public async Task Handle(StartActivityMsg message)
        {
            _logger.LogInformation($"Received {nameof(StartActivityMsg)} with\n" +
                $"{nameof(message.CorrelationId)}: {message.CorrelationId}, \n" +
                $"{nameof(message.OperationId)}: {message.OperationId}, \n" +
                $"{nameof(message.ActivityId)}: {message.ActivityId}, \n" +
                $"{nameof(message.Name)}: {message.Name}, \n" +
                $"{nameof(message.URI)}: {message.URI}, \n" +
                $"{nameof(message.StartDate)}: {message.StartDate}");

            var command = _mapper.Map<StartActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<StartActivityMsg> message)
        {
            _logger.LogError($"{nameof(StartActivityMsg)} failed with \n" +
                             $"CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<StartActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task Handle(StartActivityMsgV2 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"Received {nameof(StartActivityMsgV2)} with\n" +
                $"requestId: {_contextAccessor.GetRequestId()}, \n");

            var command = _mapper.Map<StartActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<StartActivityMsgV2> message)
        {
            _logger.LogError($"{nameof(StartActivityMsgV2)} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<StartActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

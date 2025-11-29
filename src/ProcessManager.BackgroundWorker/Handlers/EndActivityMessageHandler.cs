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
    public class EndActivityMessageHandler : IHandleMessages<EndActivityMsg>, IHandleMessages<EndActivityMsgV2>,
        IHandleMessages<IFailed<EndActivityMsg>>, IHandleMessages<IFailed<EndActivityMsgV2>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public EndActivityMessageHandler(
            IMediator mediator, 
            ILogger<EndActivityMessageHandler> logger,
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

        public async Task Handle(EndActivityMsg message)
        {
            _logger.LogInformation($"Received {nameof(EndActivityMsg)} with\n" +
                           $"{nameof(message.CorrelationId)}: {message.CorrelationId}, \n" +
                           $"{nameof(message.ActivityId)}: {message.ActivityId}, \n" +
                           $"{nameof(message.Status)}: {message.Status}, \n" +
                           $"{nameof(message.EndDate)}: {message.EndDate}, \n" +
                           $"{nameof(message.URI)}: {message.URI}");

            var command = _mapper.Map<EndActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<EndActivityMsg> message)
        {
            _logger.LogError($"{nameof(EndActivityMsg)} failed with \n" +
                             $"CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");
            await _mediator.Publish(_mapper.Map<EndActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task Handle(EndActivityMsgV2 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"Received {nameof(EndActivityMsgV2)} with\n" +
                $"requestId: {_contextAccessor.GetRequestId()}, \n");

            var command = _mapper.Map<EndActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<EndActivityMsgV2> message)
        {
            _logger.LogError($"{nameof(EndActivityMsgV2)} failed with \n" +
                             $"requestId: {_contextAccessor.GetRequestId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<EndActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

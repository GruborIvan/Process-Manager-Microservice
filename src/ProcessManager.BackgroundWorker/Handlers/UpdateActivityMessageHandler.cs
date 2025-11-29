using System;
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
    public class UpdateActivityMessageHandler : IHandleMessages<UpdateActivityMsg>, IHandleMessages<UpdateActivityMsgV2>,
        IHandleMessages<IFailed<UpdateActivityMsg>>, IHandleMessages<IFailed<UpdateActivityMsgV2>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public UpdateActivityMessageHandler(IMediator mediator,
            ILogger<UpdateActivityMessageHandler> logger,
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

        public async Task Handle(UpdateActivityMsg message)
        {
            _logger.LogInformation($"Received {nameof(UpdateActivityMsg)} with\n" +
                $"{nameof(message.CorrelationId)}: {message.CorrelationId}, \n" +
                $"{nameof(message.ActivityId)}: {message.ActivityId}, \n" +
                $"{nameof(message.Status)}: {message.Status}, \n" +
                $"{nameof(message.URI)}: {message.URI}");

            var command = _mapper.Map<UpdateActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<UpdateActivityMsg> message)
        {
            _logger.LogError($"{nameof(UpdateActivityMsg)} failed with \n" +
                             $"CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<UpdateActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task Handle(UpdateActivityMsgV2 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"Received {nameof(UpdateActivityMsgV2)} with\n" +
                $"RequestId: {_contextAccessor.GetRequestId()}, \n" +
                $"{nameof(message.ActivityId)}: {message.ActivityId}, \n" +
                $"{nameof(message.Status)}: {message.Status}, \n" +
                $"{nameof(message.URI)}: {message.URI}");

            var command = _mapper.Map<UpdateActivityCommand>(message);
            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<UpdateActivityMsgV2> message)
        {
            _logger.LogError($"{nameof(UpdateActivityMsgV2)} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<UpdateActivityDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

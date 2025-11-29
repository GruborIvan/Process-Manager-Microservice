using AutoMapper;
using FiveDegrees.Messages.Orchestrator;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.DomainEvents;
using ProcessManager.Domain.Interfaces;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class JObjectHandler : IHandleMessages<JObject>, IHandleMessages<IFailed<JObject>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public JObjectHandler(IMediator mediator, IMapper mapper, ILogger<JObjectHandler> logger, IContextAccessor contextAccessor, IBus bus)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _bus = bus;
        }

        public Task Handle(JObject message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();
            _logger.LogInformation($"{message["ProcessKey"]} message received. Id: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()}");

            var command = _mapper.Map<StartProcessCommand>(message);

            return _mediator.Send(command);            
        }

        public async Task Handle(IFailed<JObject> message)
        {
            _logger.LogError($"{message.Message["ProcessKey"]} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()} and error description: {message.ErrorDescription}.");

            var domainEvent = _mapper.Map<StartProcessDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message));
            await _mediator.Publish(domainEvent);
            var entityRelations = JsonConvert.DeserializeObject<IEnumerable<EntityRelation>>(message.Message.GetValue("EntityRelations", StringComparison.OrdinalIgnoreCase).ToString());
            await _mediator.Publish(new OperationDomainFailed(Guid.Parse(message.Message.GetValue("OperationId", StringComparison.OrdinalIgnoreCase).ToString()), new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), entityRelations?.FirstOrDefault()?.EntityType));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

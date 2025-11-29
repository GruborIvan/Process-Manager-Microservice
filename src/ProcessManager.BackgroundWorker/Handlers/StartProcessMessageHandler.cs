using System.Linq;
using FiveDegrees.Messages.Orchestrator.Interfaces;
using MediatR;
using Rebus.Handlers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoMapper;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.DomainEvents;
using ProcessManager.Domain.Interfaces;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Retry.Simple;

namespace ProcessManager.BackgroundWorker.Handlers
{
    public class StartProcessMessageHandler : IHandleMessages<IStartProcessMsg>, IHandleMessages<IStartProcessMsgV2>, IHandleMessages<IStartProcessMsgV3>,
        IHandleMessages<IFailed<IStartProcessMsg>>, IHandleMessages<IFailed<IStartProcessMsgV2>>, IHandleMessages<IFailed<IStartProcessMsgV3>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IContextAccessor _contextAccessor;
        private readonly IBus _bus;

        public StartProcessMessageHandler(
            IMediator mediator, 
            ILogger<StartProcessMessageHandler> logger,
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

        public async Task Handle(IStartProcessMsg message) => await HandleProcessMessage(message);

        public async Task Handle(IStartProcessMsgV2 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"{message.ProcessKey} message received. Id: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()}");

            var command = _mapper.Map<StartProcessCommand>(message);

            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<IStartProcessMsgV2> message)
        {
            _logger.LogError($"{message.Message.ProcessKey} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<StartProcessDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _mediator.Publish(new OperationDomainFailed(message.Message.OperationId, new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), message.Message?.EntityRelations?.FirstOrDefault()?.EntityType));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task HandleProcessMessage(IStartProcessMsg message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"{message.ProcessKey} message received. Id: {message.CorrelationId}");

            var command = _mapper.Map<StartProcessCommand>(message);

            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<IStartProcessMsg> message)
        {
            _logger.LogError($"{message.Message.ProcessKey} failed with \n" +
                             $"CorrelationId: {message.Message.CorrelationId} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<StartProcessDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _mediator.Publish(new OperationDomainFailed(message.Message.OperationId, new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), message.Message?.EntityRelations?.FirstOrDefault()?.EntityType));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }

        public async Task Handle(IStartProcessMsgV3 message)
        {
            _contextAccessor.CheckIfCommandIdAndRequestIdExists();

            _logger.LogInformation($"{message.ProcessKey} message received. Id: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()}");

            var command = _mapper.Map<StartProcessCommand>(message);

            await _mediator.Send(command);
        }

        public async Task Handle(IFailed<IStartProcessMsgV3> message)
        {
            _logger.LogError($"{message.Message.ProcessKey} failed with \n" +
                             $"RequestId: {_contextAccessor.GetRequestId()} and commandId: {_contextAccessor.GetCommandId()} and error description: {message.ErrorDescription}.");

            await _mediator.Publish(_mapper.Map<StartProcessDomainFailed>((message.Message, message.Exceptions?.FirstOrDefault()?.Message)));
            await _mediator.Publish(new OperationDomainFailed(message.Message.OperationId, new Domain.Models.ErrorData(message.Exceptions?.FirstOrDefault()?.Message, "", "process"), message.Message?.EntityRelations?.FirstOrDefault()?.EntityType));

            await _bus.Advanced.TransportMessage.Deadletter(message.ErrorDescription);
        }
    }
}

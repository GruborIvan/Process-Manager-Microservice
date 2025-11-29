using AutoMapper;
using MediatR;
using Newtonsoft.Json;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.DomainEvents
{
    public class InsertWorkflowRunDomainCompletedHandler : INotificationHandler<InsertWorkflowRunDomainCompleted>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public InsertWorkflowRunDomainCompletedHandler(IUnitOfWork unitOfWork, IContextAccessor contextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(InsertWorkflowRunDomainCompleted notification, CancellationToken cancellationToken)
        {
            var @event = _mapper.Map<InsertWorkflowRunSucceeded>(notification);
            
            var jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/UnorchestratedRun/{@event.OperationId}"
            };

            await _unitOfWork.OutboxRepository.AddAsync(_contextAccessor.GetCommandId(), OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
        }
    }
}

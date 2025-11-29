using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Newtonsoft.Json;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class OperationDomainCompletedHandler : INotificationHandler<OperationDomainCompleted>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public OperationDomainCompletedHandler(IUnitOfWork unitOfWork, IContextAccessor contextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(OperationDomainCompleted notification, CancellationToken cancellationToken)
        {
            string jsonEvent;
            if (notification.Status == "failed")
            {
                var @event = _mapper.Map<ProcessFailed>(notification);
                jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);
            }
            else
            {
                var @event = _mapper.Map<ProcessSucceeded>(notification);
                jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);
            }

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/Workflows/{notification.OperationId}"
            };

            await _unitOfWork.OutboxRepository.AddAsync(_contextAccessor.GetCommandId(), OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
        }
    }
}

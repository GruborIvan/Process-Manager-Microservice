using System;
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
    public class UpdateActivityDomainSucceededHandler : INotificationHandler<UpdateActivityDomainSucceeded>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public UpdateActivityDomainSucceededHandler(IUnitOfWork unitOfWork, IContextAccessor contextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(UpdateActivityDomainSucceeded notification, CancellationToken cancellationToken)
        {
            var @event = _mapper.Map<UpdateActivitySucceeded>(notification);

            var jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/Activities/{@event.ActivityId}"
            };
            await _unitOfWork.OutboxRepository.AddAsync(_contextAccessor.GetCommandId(), OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
        }
    }
}

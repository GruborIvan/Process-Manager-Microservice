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
    public class EndActivityDomainFailedHandler : INotificationHandler<EndActivityDomainFailed>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public EndActivityDomainFailedHandler(IUnitOfWork unitOfWork, IContextAccessor contextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(EndActivityDomainFailed notification, CancellationToken cancellationToken)
        {
            var @event = _mapper.Map<EndActivityFailed>(notification);

            var jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/Activities/{@event.ActivityId}"
            };
            await _unitOfWork.OutboxRepository.AddAsync(_contextAccessor.GetCommandId(), OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

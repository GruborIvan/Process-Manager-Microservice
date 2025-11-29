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
    public class OperationDomainFailedHandler : INotificationHandler<OperationDomainFailed>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public OperationDomainFailedHandler(IUnitOfWork unitOfWork, IContextAccessor contextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _mapper = mapper;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(OperationDomainFailed notification, CancellationToken cancellationToken)
        {
            await TryToUpdateWorkflowRunStatusToFailed(notification.OperationId, cancellationToken);

            var @event = _mapper.Map<ProcessFailed>(notification);
            var jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/Workflows/{notification.OperationId}"
            };

            await _unitOfWork.OutboxRepository.AddAsync(_contextAccessor.GetCommandId(), OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> TryToUpdateWorkflowRunStatusToFailed(Guid operationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingWorkflow = await _unitOfWork.WorkflowRepository.GetAsync(operationId, cancellationToken);
                existingWorkflow.UpdateWorkflowStatus("failed");
                _unitOfWork.WorkflowRepository.Update(existingWorkflow);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

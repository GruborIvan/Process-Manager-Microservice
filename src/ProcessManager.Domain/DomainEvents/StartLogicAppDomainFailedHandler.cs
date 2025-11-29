using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;

namespace ProcessManager.Domain.DomainEvents
{
    public class StartLogicAppDomainFailedHandler : INotificationHandler<StartLogicAppDomainFailed>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IConfiguration _configuration;

        public StartLogicAppDomainFailedHandler(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task Handle(StartLogicAppDomainFailed notification, CancellationToken cancellationToken)
        {
            var outboxMessage = notification.OutboxMessage;
            var maxRetryNumber = _configuration.GetSection("ProcessManagerConfiguration")
                .GetValue<int>("StartLogicAppMaxRetryNumber");

            if (outboxMessage.RetryAttempt.GetValueOrDefault() < maxRetryNumber)
            {
                var nextRetryAttempt = outboxMessage.RetryAttempt.GetValueOrDefault() + 1;
                outboxMessage.NextRetryDate = NextRetryDate(nextRetryAttempt);
                outboxMessage.RetryAttempt = nextRetryAttempt;
            }
            else
            {
                outboxMessage.ProcessedDate = DateTime.UtcNow;

                var jsonLaEvent = JObject.Parse(outboxMessage.Data);
                var startProcessSucceeded = JsonConvert.DeserializeObject<StartProcessSucceeded>(jsonLaEvent["data"].ToString(), _jsonSerializerSettings);

                var existingWorkflow = await UpdateWorkflowRunStatusToFailed(startProcessSucceeded.OperationId, cancellationToken);
                await AddProcessFailedEvent(startProcessSucceeded, notification.Message, existingWorkflow.Relations?.FirstOrDefault()?.EntityType, outboxMessage.MessageId, cancellationToken);
            }
            _unitOfWork.OutboxRepository.Update(outboxMessage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task AddProcessFailedEvent(StartProcessSucceeded startProcessSucceeded, string errorMessage, string entityType, Guid messageId, CancellationToken cancellationToken)
        {
            var @event = new ProcessFailed
            (
                startProcessSucceeded.CorrelationId,
                startProcessSucceeded.RequestId,
                startProcessSucceeded.CommandId,
                startProcessSucceeded.OperationId,
                new ErrorData(errorMessage, "", "process"),
                entityType
            );

            var jsonEvent = JsonConvert.SerializeObject(@event, _jsonSerializerSettings);

            var eventGridEvent = new
            {
                Data = jsonEvent,
                Subject = $"api/Workflows/{@event.OperationId}"
            };

            await _unitOfWork.OutboxRepository.AddAsync(messageId, OutboxMessageType.EventGrid, JsonConvert.SerializeObject(eventGridEvent), cancellationToken);
        }

        private async Task<WorkflowRun> UpdateWorkflowRunStatusToFailed(Guid operationId,
            CancellationToken cancellationToken)
        {
            var existingWorkflow =
                await _unitOfWork.WorkflowRepository.GetAsync(operationId, cancellationToken);
            existingWorkflow.UpdateWorkflowStatus("failed");
            _unitOfWork.WorkflowRepository.Update(existingWorkflow);
            return existingWorkflow;
        }

        private DateTime NextRetryDate(int retryAttempt)
        {
            var retryDelayInSec = _configuration.GetSection("ProcessManagerConfiguration")
                .GetValue<int>("StartLogicAppInitialDelayInSec");
            var delay = retryDelayInSec * (Math.Pow(2, retryAttempt) - 1);
            return DateTime.UtcNow.AddSeconds(delay);
        }
    }
}

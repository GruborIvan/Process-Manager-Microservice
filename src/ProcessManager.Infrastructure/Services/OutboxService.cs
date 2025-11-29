using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.DomainEvents;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;

namespace ProcessManager.Infrastructure.Services
{
    public class OutboxService : IOutboxService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IEventNotificationService _eventNotificationService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IProcessService _processService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public OutboxService(IUnitOfWork unitOfWork, IWorkflowRepository workflowRepository, IEventNotificationService eventNotificationService, IProcessService processService, IMediator mediator, ILogger<OutboxService> logger)
        {
            _unitOfWork = unitOfWork;
            _workflowRepository = workflowRepository;
            _eventNotificationService = eventNotificationService;
            _processService = processService;
            _mediator = mediator;
            _logger = logger;
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public async Task SendEventsAsync(CancellationToken ct = default)
        {
            var unprocessedEvents = await _unitOfWork.OutboxRepository.GetUnprocessedEventsAsync(ct);
            foreach (var unprocessedEvent in unprocessedEvents)
            {
                try
                {
                    if (unprocessedEvent.Type == OutboxMessageType.EventGrid)
                    {
                        var jsonEvent = JObject.Parse(unprocessedEvent.Data);
                        var eventData = jsonEvent["data"];
                        var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
                        var subject = jsonEvent["subject"];
                        var x = eventData.Children();

                        _logger.LogInformation($"Publishing Event: {@event.GetType().Name} with RequestId: {@event.GetType().GetProperty("RequestId").GetValue(@event).ToString()}");
                        await _eventNotificationService.SendAsync(@event, subject.ToString());
                    }
                    unprocessedEvent.ProcessMessage();

                    _unitOfWork.OutboxRepository.Update(unprocessedEvent);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SendEventsAsync, message {unprocessedEvent?.MessageId} error: {ex.Message}");
                }
            }
        }

        public async Task StartLogicAppsAsync(CancellationToken ct = default)
        {
            var logicAppMessages = await _unitOfWork.OutboxRepository.GetLogicAppStartMessagesAsync(ct);
            
            foreach(var logicAppMessage in logicAppMessages)
            {
                try
                {
                    var json = JObject.Parse(logicAppMessage.Data);
                    var process = json["process"];
                    var headers = json["headers"];
                    var messageHeaders = headers.ToObject<Dictionary<string, string>>();
                    var startMessage = JsonConvert.DeserializeObject<Process>(process.ToString());

                    Guid operationId = Guid.Parse(process?["parameters"]?["operationId"].ToString());

                    _logger.LogInformation($"Starting Logic App:  {startMessage.Key} with RequestId: {messageHeaders["x-request-id"]}");
                    string workflowRunId = await _processService.StartProcessAsync(startMessage, messageHeaders);

                    await _workflowRepository.UpdateWorkflowId(operationId, workflowRunId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"StartLogicAppsAsync, message {logicAppMessage?.MessageId} error: {ex.Message}");
                    await _mediator.Publish(new StartLogicAppDomainFailed(logicAppMessage, ex.Message, new ErrorData(ex.Message, "", "process")), ct);
                    continue;
                }
                logicAppMessage.ProcessMessage();
                _unitOfWork.OutboxRepository.Update(logicAppMessage);
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Messages.ProcessManager;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public class UpdateProcessMessageHandlerTests : TestFixture
    {
        private readonly EventWaitHandle _updateProcessMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public UpdateProcessMessageHandlerTests()
        {
            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<UpdateProcessStatusMsg>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<UpdateProcessStatusMsg>>>();

            var publisherActivator = new BuiltinHandlerActivator();
            var subscriberActivator = new BuiltinHandlerActivator();
            subscriberActivator.Register(x => handler);
            subscriberActivator.Register(x => failedHandler);

            var subscriberStore = new InMemorySubscriberStore();
            var network = new InMemNetwork();
            var queueName = "test";

            _subscriber = Configure.With(subscriberActivator)
                    .Transport(t => t.UseInMemoryTransport(network, queueName))
                    .Subscriptions(s => s.StoreInMemory(subscriberStore))
                    .Options(b => b.SimpleRetryStrategy(maxDeliveryAttempts: 1, secondLevelRetriesEnabled: true))
                    .Events(e =>
                    {
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _updateProcessMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<UpdateProcessStatusMsg>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task ValidMessage_UpdatesProcess_With_Completed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "completed";
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;
            var correlationId = Guid.NewGuid();
            var expectedWorkflowDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "RunName",
                Status = startingStatus,

                CreatedBy = "test",
                CreatedDate = DateTime.Now,
                ChangedBy = "test", 
                ChangedDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.Add(expectedWorkflowDbo);
            await context.SaveChangesAsync();

            // Act
            var updateProcessMsg = new UpdateProcessStatusMsg(correlationId, expectedOperationId, "succeeded", expectedDate);
            await _publisher.Publish(updateProcessMsg, GetValidHeaders());

            // Assert
            _updateProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var updatedWorkflow = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == expectedOperationId);
            var outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("UpdateProcessStatusSucceeded"));
            var outboxMessageProcessEvent = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("ProcessSucceeded"));

            Assert.NotNull(updatedWorkflow);
            Assert.Equal(expectedStatus, updatedWorkflow.Status);

            var jsonEvent = JObject.Parse(outboxMessageUpdateProcessStatus.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageUpdateProcessStatus.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("UpdateProcessStatusSucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((UpdateProcessStatusSucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessageUpdateProcessStatus.MessageId);

            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessSucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((ProcessSucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessageProcessEvent.MessageId);
        }

        [Fact]
        public async Task ValidMessage_UpdatesProcess_With_Failed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "failed";
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;
            var correlationId = Guid.NewGuid();
            var expectedWorkflowDbo = new WorkflowRunDbo
            {
                OperationId = expectedOperationId,
                WorkflowRunName = "RunName",
                Status = startingStatus,

                CreatedBy = "test",
                CreatedDate = DateTime.Now,
                ChangedBy = "test",
                ChangedDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.Add(expectedWorkflowDbo);
            await context.SaveChangesAsync();

            // Act
            var updateProcessMsg = new UpdateProcessStatusMsg(correlationId, expectedOperationId, "failed", expectedDate);
            await _publisher.Publish(updateProcessMsg, GetValidHeaders());

            // Assert
            _updateProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var updatedWorkflow = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == expectedOperationId);
            var outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("UpdateProcessStatusSucceeded"));
            var outboxMessageProcessEvent = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("ProcessFailed"));

            Assert.NotNull(updatedWorkflow);
            Assert.Equal(expectedStatus, updatedWorkflow.Status);

            var jsonEvent = JObject.Parse(outboxMessageUpdateProcessStatus.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageUpdateProcessStatus.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("UpdateProcessStatusSucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((UpdateProcessStatusSucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessageUpdateProcessStatus.MessageId);

            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessFailed", @event.GetType().Name);
            Assert.Equal(correlationId, ((ProcessFailed)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessageProcessEvent.MessageId);
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_UpdateProcess()
        {
            // Arrange 
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;
            var correlationId = Guid.NewGuid();

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var updateProcessMsg = new UpdateProcessStatusMsg(correlationId, expectedOperationId, "succeeded", expectedDate);
            await _publisher.Publish(updateProcessMsg, GetValidHeaders());

            // Assert
            _updateProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(1000);

            var updatedWorkflow = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == expectedOperationId);
            var outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("UpdateProcessStatusFailed"));
            var outboxMessageProcessEvent = context.OutboxMessages.SingleOrDefault(x => x.Data.Contains("ProcessFailed"));

            Assert.Null(updatedWorkflow);

            var jsonEvent = JObject.Parse(outboxMessageUpdateProcessStatus.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageUpdateProcessStatus.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("UpdateProcessStatusFailed", @event.GetType().Name);
            Assert.Equal(correlationId, ((UpdateProcessStatusFailed)@event).CorrelationId);
            Assert.Equal($"Workflow with operationId: {expectedOperationId} not found.", ((UpdateProcessStatusFailed)@event).Error.Message);
            Assert.Equal(Guid.Empty, outboxMessageUpdateProcessStatus.MessageId);

            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessFailed", @event.GetType().Name);
            Assert.Equal(correlationId, ((ProcessFailed)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessageProcessEvent.MessageId);
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }

        private Dictionary<string, string> GetValidHeaders()
        {
            return new Dictionary<string, string>
            {
                {
                    "x-request-id", _requestId.ToString()
                }
            };
        }
    }
}

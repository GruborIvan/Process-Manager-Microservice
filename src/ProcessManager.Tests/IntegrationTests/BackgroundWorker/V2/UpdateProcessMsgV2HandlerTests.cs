using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Messages.ProcessManager.v2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.BackgroundWorker.Extensions;
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

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker.V2
{
    public class UpdateProcessMsgV2HandlerTests : TestFixture
    {
        private readonly EventWaitHandle _updateProcessMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly InMemNetwork _network;
        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public UpdateProcessMsgV2HandlerTests()
        {
            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ProcessManager.BackgroundWorker.SendEvents.Worker>();
                });

            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<UpdateProcessStatusMsgV2>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<UpdateProcessStatusMsgV2>>>();

            var publisherActivator = new BuiltinHandlerActivator();
            var subscriberActivator = new BuiltinHandlerActivator();
            subscriberActivator.Register(x => handler);
            subscriberActivator.Register(x => failedHandler);

            var subscriberStore = new InMemorySubscriberStore();
            _network = new InMemNetwork();
            var queueName = "test";

            _subscriber = Configure.With(subscriberActivator)
                    .Transport(t => t.UseInMemoryTransport(_network, queueName))
                    .Subscriptions(s => s.StoreInMemory(subscriberStore))
                    .Options(o =>
                    {
                        o.UseContainerScopeInitializerStep(_host.Services.GetAutofacRoot().Resolve<ILifetimeScope>());
                        o.EnableMessageDeDuplication();
                        o.SimpleRetryStrategy(maxDeliveryAttempts: 1, secondLevelRetriesEnabled: true);
                        o.UseFailFastChecker();
                    })
                    .Events(e =>
                    {
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _updateProcessMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(_network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<UpdateProcessStatusMsgV2>().GetAwaiter().GetResult();

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
            var updateProcessMsg = new UpdateProcessStatusMsgV2(expectedOperationId, "succeeded", expectedDate);
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
            Assert.Equal(_commandId, ((UpdateProcessStatusSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateProcessStatusSucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessageUpdateProcessStatus.MessageId);
            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessSucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((ProcessSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((ProcessSucceeded)@event).RequestId);

            //mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(2500);
            outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null && x.Data.Contains("UpdateProcessStatusSucceeded"));
            Assert.Null(outboxMessageUpdateProcessStatus);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ValidMessage_UpdatesProcess_With_Failed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "failed";
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;
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
            var updateProcessMsg = new UpdateProcessStatusMsgV2(expectedOperationId, "failed", expectedDate);
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
            Assert.Equal(_commandId, ((UpdateProcessStatusSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateProcessStatusSucceeded)@event).RequestId);

            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessFailed", @event.GetType().Name);
            Assert.Equal(_commandId, ((ProcessFailed)@event).CommandId);
            Assert.Equal(_requestId, ((ProcessFailed)@event).RequestId);

            Assert.Equal(_commandId, outboxMessageUpdateProcessStatus.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(2500);
            outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null && x.Data.Contains("UpdateProcessStatusSucceeded"));
            Assert.Null(outboxMessageUpdateProcessStatus);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_UpdateProcess()
        {
            // Arrange 
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var updateProcessMsg = new UpdateProcessStatusMsgV2(expectedOperationId, "succeeded", expectedDate);
            await _publisher.Publish(updateProcessMsg, GetValidHeaders());

            // Assert
            _updateProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(500);

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
            Assert.Equal(_commandId, ((UpdateProcessStatusFailed)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateProcessStatusFailed)@event).RequestId);
            Assert.Equal($"Workflow with operationId: {expectedOperationId} not found.", ((UpdateProcessStatusFailed)@event).Error.Message);
            Assert.Equal(_commandId, outboxMessageUpdateProcessStatus.MessageId);
            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessFailed", @event.GetType().Name);
            Assert.Equal(_commandId, ((ProcessFailed)@event).CommandId);
            Assert.Equal(_requestId, ((ProcessFailed)@event).RequestId);
            //mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(1500);
            outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null && x.Data.Contains("UpdateProcessStatusFailed"));
            Assert.Null(outboxMessageUpdateProcessStatus);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SameMessageSentTwoTimes_SecondMessageThrowsError()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "completed";
            var expectedDate = DateTime.UtcNow;
            var expectedOperationId = _requestId;
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
            var updateProcessMsg = new UpdateProcessStatusMsgV2(expectedOperationId, "succeeded", expectedDate);
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
            Assert.Equal(_commandId, ((UpdateProcessStatusSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateProcessStatusSucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessageUpdateProcessStatus.MessageId);
            jsonEvent = JObject.Parse(outboxMessageProcessEvent.Data);
            eventData = jsonEvent["data"];
            @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessageProcessEvent.Type);
            Assert.Equal($"api/Workflows/{expectedOperationId}", subject);
            Assert.Equal("ProcessSucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((ProcessSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((ProcessSucceeded)@event).RequestId);

            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //if we send message with the same commandId, then second message shouldn't be processed
            //this message will be moved to error queue
            var secondUpdateProcessMsg = new UpdateProcessStatusMsgV2(expectedOperationId, "new status", expectedDate);
            await _publisher.Publish(secondUpdateProcessMsg, GetValidHeaders());
            _updateProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(2500);

            updatedWorkflow = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == expectedOperationId);
            outboxMessageUpdateProcessStatus = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null && x.Data.Contains("UpdateProcessStatusSucceeded"));
            Assert.Null(outboxMessageUpdateProcessStatus);
            Assert.Equal(expectedStatus, updatedWorkflow.Status);

            var messageFromErrorQueue = _network.GetNextOrNull("error");
            var errorMessage = messageFromErrorQueue.Headers["rbs2-error-details"];
            Assert.Contains($"Message UpdateProcessStatusMsgV2 with Id {_commandId} already exists.", errorMessage);
            var jsonMessage = JObject.Parse(System.Text.Encoding.Default.GetString(messageFromErrorQueue.Body));
            var message = JsonConvert.DeserializeObject(jsonMessage.ToString(), _jsonSerializerSettings);
            Assert.Equal("UpdateProcessStatusMsgV2", message.GetType().Name);
            Assert.Equal(expectedOperationId, ((UpdateProcessStatusMsgV2)message).OperationId);
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
                },
                {
                    "x-command-id", _commandId.ToString()
                }
            };
        }
    }
}

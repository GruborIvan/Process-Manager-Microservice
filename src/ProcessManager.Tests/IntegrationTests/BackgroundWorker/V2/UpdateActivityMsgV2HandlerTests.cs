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
using ProcessManager.Domain.Interfaces;
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
    public class UpdateActivityMsgV2HandlerTests : TestFixture
    {
        private readonly EventWaitHandle _updateActivityMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 20000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly InMemNetwork _network;
        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public UpdateActivityMsgV2HandlerTests()
        {
            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ProcessManager.BackgroundWorker.SendEvents.Worker>();
                });

            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<UpdateActivityMsgV2>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<UpdateActivityMsgV2>>>();

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
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _updateActivityMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(_network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<UpdateActivityMsgV2>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task ValidMessage_UpdatesActivity_With_Completed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "completed";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = Guid.NewGuid();
            var expectedUri = "test";
            var activityDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = expectedOperationId,
                Status = startingStatus,
                URI = expectedUri,
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var startActivityMsg = new UpdateActivityMsgV2(
                expectedActivityId, "succeeded", expectedUri);

            await _publisher.Publish(startActivityMsg, GetValidHeaders());

            _updateActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var updatedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(updatedActivity);
            Assert.Equal(activityDbo.Name, updatedActivity.Name);
            Assert.Equal(expectedActivityId, updatedActivity.ActivityId);
            Assert.Equal(expectedOperationId, updatedActivity.OperationId);
            Assert.Equal(expectedStatus, updatedActivity.Status);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("UpdateActivitySucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((UpdateActivitySucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateActivitySucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(2500);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ValidMessage_UpdatesActivity_With_Failed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "failed";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = Guid.NewGuid();
            var expectedUri = "test";
            var activityDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = expectedOperationId,
                Status = startingStatus,
                URI = expectedUri,
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var startActivityMsg = new UpdateActivityMsgV2(
                expectedActivityId, "failed", expectedUri);

            await _publisher.Publish(startActivityMsg, GetValidHeaders());

            _updateActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var updatedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(updatedActivity);
            Assert.Equal(activityDbo.Name, updatedActivity.Name);
            Assert.Equal(expectedActivityId, updatedActivity.ActivityId);
            Assert.Equal(expectedOperationId, updatedActivity.OperationId);
            Assert.Equal(expectedStatus, updatedActivity.Status);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("UpdateActivitySucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((UpdateActivitySucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateActivitySucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(2000);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_Update_Activity()
        {
            // Arrange 
            var startingStatus = "started";
            var completedStatus = "completed";
            var invalidActivityId = Guid.Empty;
            var activityDbo = new ActivityDbo
            {
                ActivityId = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                Status = startingStatus,
                URI = "test",
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var updateActivityMsg = new UpdateActivityMsgV2(
                invalidActivityId, completedStatus, "test");

            await _publisher.Publish(updateActivityMsg, GetValidHeaders());

            _updateActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(500);

            var updatedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == invalidActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.Null(updatedActivity);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{invalidActivityId}", subject);
            Assert.Equal("UpdateActivityFailed", @event.GetType().Name);
            Assert.Equal(_commandId, ((UpdateActivityFailed)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateActivityFailed)@event).RequestId);
            Assert.Contains("must not be empty", ((UpdateActivityFailed)@event).Error.Message);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(1500);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SameMessageSentTwoTimes_SecondMessageThrowsError()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "completed";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = Guid.NewGuid();
            var expectedUri = "test";
            var activityDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = expectedOperationId,
                Status = startingStatus,
                URI = expectedUri,
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var updateActivityMsgV2 = new UpdateActivityMsgV2(
                expectedActivityId, "succeeded", expectedUri);

            await _publisher.Publish(updateActivityMsgV2, GetValidHeaders());

            _updateActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var updatedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(updatedActivity);
            Assert.Equal(activityDbo.Name, updatedActivity.Name);
            Assert.Equal(expectedActivityId, updatedActivity.ActivityId);
            Assert.Equal(expectedOperationId, updatedActivity.OperationId);
            Assert.Equal(expectedStatus, updatedActivity.Status);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("UpdateActivitySucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((UpdateActivitySucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((UpdateActivitySucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //if we send message with the same commandId, then second message shouldn't be processed
            //this message will be moved to error queue
            var secondUpdateActivityMsgV2 = new UpdateActivityMsgV2(expectedActivityId, "new status", expectedUri);
            await _publisher.Publish(secondUpdateActivityMsgV2, GetValidHeaders());
            _updateActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(2500);

            updatedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            Assert.Equal(expectedStatus, updatedActivity.Status);

            var messageFromErrorQueue = _network.GetNextOrNull("error");
            var errorMessage = messageFromErrorQueue.Headers["rbs2-error-details"];
            Assert.Contains($"Message UpdateActivityMsgV2 with Id {_commandId} already exists.", errorMessage);
            var jsonMessage = JObject.Parse(System.Text.Encoding.Default.GetString(messageFromErrorQueue.Body));
            var message = JsonConvert.DeserializeObject(jsonMessage.ToString(), _jsonSerializerSettings);
            Assert.Equal("UpdateActivityMsgV2", message.GetType().Name);
            Assert.Equal(expectedActivityId, ((UpdateActivityMsgV2)message).ActivityId);
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

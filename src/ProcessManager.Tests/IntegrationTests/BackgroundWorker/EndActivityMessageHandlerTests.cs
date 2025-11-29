using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Messages.ProcessManager;
using Microsoft.Extensions.Hosting;
using ProcessManager.Infrastructure.Models;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Models;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public class EndActivityMessageHandlerTests : TestFixture
    {
        private readonly EventWaitHandle _endActivityMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 20000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public EndActivityMessageHandlerTests()
        {
            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<EndActivityMsg>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<EndActivityMsg>>>();

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
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _endActivityMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<EndActivityMsg>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task ValidMessage_EndsActivity_With_Completed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "completed";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var activityDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = expectedOperationId,
                Status = startingStatus,
                URI = "test/test",
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var endActivityMsg = new EndActivityMsg(
                correlationId, expectedActivityId, "succeeded", DateTime.Now);

            await _publisher.Publish(endActivityMsg);

            _endActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var endedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(endedActivity);
            Assert.Equal(activityDbo.Name, endedActivity.Name);
            Assert.Equal(expectedActivityId, endedActivity.ActivityId);
            Assert.Equal(expectedOperationId, endedActivity.OperationId);
            Assert.Equal(expectedStatus, endedActivity.Status);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("EndActivitySucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((EndActivitySucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessage.MessageId);
        }

        [Fact]
        public async Task ValidMessage_EndsActivity_With_Failed_Status()
        {
            // Arrange 
            var startingStatus = "started";
            var expectedStatus = "failed";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var activityDbo = new ActivityDbo
            {
                ActivityId = expectedActivityId,
                OperationId = expectedOperationId,
                Status = startingStatus,
                URI = "test/test",
                Name = "test",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.Activities.Add(activityDbo);
            await context.SaveChangesAsync();

            // Act
            var endActivityMsg = new EndActivityMsg(
                correlationId, expectedActivityId, "failed", DateTime.Now);

            await _publisher.Publish(endActivityMsg);

            _endActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var endedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(endedActivity);
            Assert.Equal(activityDbo.Name, endedActivity.Name);
            Assert.Equal(expectedActivityId, endedActivity.ActivityId);
            Assert.Equal(expectedOperationId, endedActivity.OperationId);
            Assert.Equal(expectedStatus, endedActivity.Status);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("EndActivitySucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((EndActivitySucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessage.MessageId);
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_End_Activity()
        {
            // Arrange 
            var startingStatus = "started";
            var completedStatus = "completed";
            var invalidActivityId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var activityId = Guid.NewGuid();
            var activityDbo = new ActivityDbo
            {
                ActivityId = activityId,
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
            var endActivityMsg = new EndActivityMsg(
                correlationId, invalidActivityId, completedStatus, DateTime.Now, "test");

            await _publisher.Publish(endActivityMsg);
            _endActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(1000);

            var endedActivity = context.Activities.FirstOrDefault(x => x.ActivityId == invalidActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.Null(endedActivity);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{invalidActivityId}", subject);
            Assert.Equal("EndActivityFailed", @event.GetType().Name);
            Assert.Equal(correlationId, ((EndActivityFailed)@event).CorrelationId);
            Assert.Equal($"Activity with activityId: {invalidActivityId} not found.", ((EndActivityFailed)@event).Error.Message);
            Assert.Equal(Guid.Empty, outboxMessage.MessageId);
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }
    }
}

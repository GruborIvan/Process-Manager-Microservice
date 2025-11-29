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
using System.Collections.Generic;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public class StartActivityMessageHandlerTests : TestFixture
    {
        private readonly EventWaitHandle _startActivityMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public StartActivityMessageHandlerTests()
        {
            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<StartActivityMsg>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<StartActivityMsg>>>();

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
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _startActivityMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<StartActivityMsg>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task ValidMessage_SavesActivity()
        {
            // Arrange 
            var startingStatus = "in progress";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = _requestId;
            var correlationId = Guid.NewGuid();

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var startActivityMsg = new StartActivityMsg(
                correlationId, expectedOperationId, expectedActivityId, startingStatus, DateTime.Now);

            await _publisher.Publish(startActivityMsg, GetValidHeaders());

            _startActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            
            var activity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(activity);
            Assert.Equal(startingStatus, activity.Status);
            Assert.Equal(expectedActivityId, activity.ActivityId);
            Assert.Equal(expectedOperationId, activity.OperationId);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("StartActivitySucceeded", @event.GetType().Name);
            Assert.Equal(correlationId, ((StartActivitySucceeded)@event).CorrelationId);
            Assert.Equal(Guid.Empty, outboxMessage.MessageId);
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_SaveActivity()
        {
            // Arrange 
            var expectedActivityId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var invalidStartActivityMsg = new StartActivityMsg(
                correlationId, Guid.Empty, expectedActivityId, null, DateTime.MinValue);

            await _publisher.Publish(invalidStartActivityMsg);

            _startActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(1000);

            var activity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.Null(activity);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal($"api/Activities/{expectedActivityId}", subject);
            Assert.Equal("StartActivityFailed", @event.GetType().Name);
            Assert.Equal(correlationId, ((StartActivityFailed)@event).CorrelationId);
            Assert.Contains("must not be empty", ((StartActivityFailed)@event).Error.Message);
            Assert.Equal(Guid.Empty, outboxMessage.MessageId);
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

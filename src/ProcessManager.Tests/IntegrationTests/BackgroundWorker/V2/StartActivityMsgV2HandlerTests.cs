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
    public class StartActivityMsgV2HandlerTests : TestFixture
    {
        private readonly EventWaitHandle _startActivityMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly InMemNetwork _network;
        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public StartActivityMsgV2HandlerTests()
        {
            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ProcessManager.BackgroundWorker.SendEvents.Worker>();
                });

            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<StartActivityMsgV2>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<StartActivityMsgV2>>>();

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
                        e.AfterMessageHandled += (bus, headers, message, context, args) => _startActivityMsgReceived.Set();
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(_network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<StartActivityMsgV2>().GetAwaiter().GetResult();

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

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var startActivityMsg = new StartActivityMsgV2(
                expectedOperationId, expectedActivityId, startingStatus, DateTime.Now);

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
            Assert.Equal(_commandId, ((StartActivitySucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((StartActivitySucceeded)@event).RequestId);
            //Assert.Equal(_commandId, outboxMessage.MessageId);
            //mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //first we saved event which we want to send, and then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(2500);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task InvalidMessage_Doesnt_SaveActivity()
        {
            // Arrange 
            var expectedActivityId = Guid.NewGuid();

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var invalidStartActivityMsg = new StartActivityMsgV2(
               Guid.Empty, expectedActivityId, null, DateTime.MinValue);

            await _publisher.Publish(invalidStartActivityMsg, GetValidHeaders());

            _startActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(500);

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
            Assert.Equal(_commandId, ((StartActivityFailed)@event).CommandId);
            Assert.Equal(_requestId, ((StartActivityFailed)@event).RequestId);
            Assert.Contains("must not be empty", ((StartActivityFailed)@event).Error.Message);
            //Assert.Equal(_commandId, outboxMessage.MessageId);
            //mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

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
            var startingStatus = "in progress";
            var expectedActivityId = Guid.NewGuid();
            var expectedOperationId = _requestId;

            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            var startActivityMsg = new StartActivityMsgV2(
                expectedOperationId, expectedActivityId, startingStatus, DateTime.Now);

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
            Assert.Equal(_commandId, ((StartActivitySucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((StartActivitySucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Never);

            //if we send message with the same commandId, then second message shouldn't be processed
            //this message will be moved to error queue
            var secondStartActivityMsg = new StartActivityMsgV2(expectedOperationId, expectedActivityId, "new status", DateTime.Now);
            await _publisher.Publish(secondStartActivityMsg, GetValidHeaders());
            _startActivityMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(2500);

            activity = context.Activities.FirstOrDefault(x => x.ActivityId == expectedActivityId);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            Assert.Equal(startingStatus, activity.Status);

            var messageFromErrorQueue = _network.GetNextOrNull("error");
            var errorMessage = messageFromErrorQueue.Headers["rbs2-error-details"];
            Assert.Contains($"Message StartActivityMsgV2 with Id {_commandId} already exists.", errorMessage);
            var jsonMessage = JObject.Parse(System.Text.Encoding.Default.GetString(messageFromErrorQueue.Body));
            var message = JsonConvert.DeserializeObject(jsonMessage.ToString(), _jsonSerializerSettings);
            Assert.Equal("StartActivityMsgV2", message.GetType().Name);
            Assert.Equal(expectedActivityId, ((StartActivityMsgV2)message).ActivityId);
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

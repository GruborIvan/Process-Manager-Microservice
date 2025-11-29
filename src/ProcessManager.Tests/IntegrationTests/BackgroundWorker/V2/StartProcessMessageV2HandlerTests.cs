using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Messages.Orchestrator;
using FiveDegrees.Messages.Orchestrator.Interfaces;
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
    public class StartProcessMessageV2HandlerTests : TestFixture
    {
        private readonly Mock<IProcessService> _mockProcessManager;
        private readonly Mock<IFeatureFlagService> _featureFlagService;
        private readonly EventWaitHandle _startProcessMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly InMemNetwork _network;
        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public StartProcessMessageV2HandlerTests()
        {
            _mockProcessManager = new Mock<IProcessService>();
            _mockProcessManager.Setup(service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new Process { Key = "asd", Parameters = JObject.Parse("{ 'operationId' : '8C27E0E2-8AB6-4BA7-8540-396C78982A54' }"), StartUrl = "url" })
                .Verifiable();
            _mockProcessManager.Setup(service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();
            _mockProcessManager.Setup(service => service.StartProcessAsync(It.IsAny<Process>(), It.IsAny<Dictionary<string, string>>()))
                .Verifiable();

            _featureFlagService = new Mock<IFeatureFlagService>();
            _featureFlagService
                .Setup(m => m.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterInstance(_mockProcessManager.Object).As<IProcessService>();
                    builder.RegisterInstance(_featureFlagService.Object).As<IFeatureFlagService>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ProcessManager.BackgroundWorker.SendEvents.Worker>();
                    services.AddHostedService<ProcessManager.BackgroundWorker.StartLogicApps.Worker>();
                });

            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<IStartProcessMsgV2>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<IStartProcessMsgV2>>>();

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
                    e.AfterMessageHandled += (bus, headers, message, context, args) => _startProcessMsgReceived.Set();
                })
                .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(_network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<StartCreateTenantMsgV2>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task SendCreateTenantCommandToServiceBus_Valid()
        {
            var operationId = _requestId;
            var createTenantMsg = new StartCreateTenantMsgV2(
                "name",
                "sname",
                "westeurope",
                null,
                Guid.NewGuid(),
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                null,
                operationId
                );

            var workflowDbo = new WorkflowRunDbo()
            {
                OperationId = Guid.Parse("8c27e0e2-8ab6-4ba7-8540-396c78982a54"),
                WorkflowRunName = "Create-Person",
                Status = "in progress",
                WorkflowRunId = Guid.NewGuid().ToString(),
                EndDate = DateTime.Now,
                CreatedBy = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.Now,
                ChangedBy = Guid.NewGuid().ToString(),
                ChangedDate = DateTime.Now
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.Add(workflowDbo);
            context.SaveChanges();

            await _publisher.Publish(createTenantMsg, GetValidHeaders());
            _startProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var process = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == operationId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(process);
            Assert.Equal("in progress", process.Status);
            Assert.Equal(createTenantMsg.OperationId, process.OperationId);
            Assert.Equal(createTenantMsg.ProcessKey, process.WorkflowRunName);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.LogicApp, outboxMessage.Type);
            Assert.Equal($"api/Workflows/{operationId}", subject);
            Assert.Equal("StartProcessSucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((StartProcessSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((StartProcessSucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            _mockProcessManager.Verify(x => x.GetPrincipalIdAsync(createTenantMsg.ProcessKey, null), Times.Once());
            _mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(createTenantMsg.ProcessKey, createTenantMsg.ProcessName, It.IsAny<object>(), It.IsAny<string>()), Times.Once());
            _featureFlagService.Verify(x => x.GetFeatureFlagsAsync(createTenantMsg.ProcessKey, It.IsAny<CancellationToken>()), Times.Once());

            //first we start save message for start LogicApp to outbox table, when another process start LA and save event to outbox table
            //then another process pickup this event from OutboxMessages table and send event grid event
            await Task.Delay(4000);
            outboxMessage = context.OutboxMessages.SingleOrDefault(x => x.ProcessedDate == null);
            Assert.Null(outboxMessage);
            _mockProcessManager.Verify(x => x.StartProcessAsync(It.IsAny<Process>(), It.Is<Dictionary<string, string>>(x => x["x-request-id"] == _requestId.ToString() && x["x-command-id"] == _commandId.ToString())), Times.Once());
            mockEventGridService.Verify(x => x.SendAsync(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAlreadyExists_Doesnt_CreateProcess()
        {
            var operationId = _requestId;
            var createTenantMsg = new StartCreateTenantMsgV2(
                "name",
                "sname",
                "westeurope",
                null,
                Guid.NewGuid(),
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                null,
                operationId
            );
            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.Add(new WorkflowRunDbo { OperationId = operationId });
            await context.SaveChangesAsync();

            await _publisher.Publish(createTenantMsg, GetValidHeaders());
            _startProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(1000);

            var process = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == operationId);
            var outboxMessages = context.OutboxMessages.ToList();

            // Assert
            Assert.NotNull(process);
            Assert.Equal(1, outboxMessages.Count(x => x.Data.Contains("Events.StartProcessFailed")));
            Assert.Equal(1, outboxMessages.Count(x => x.Data.Contains("Events.ProcessFailed")));

            foreach (var outboxMessage in outboxMessages)
            {
                var jsonEvent = JObject.Parse(outboxMessage.Data);
                var eventData = jsonEvent["data"];
                var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
                var subject = jsonEvent["subject"];
                Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
                Assert.Equal($"api/Workflows/{operationId}", subject);

                if (@event.GetType().Name == "StartProcessFailed")
                {
                    Assert.Equal("StartProcessFailed", @event.GetType().Name);
                    Assert.Equal(_commandId, ((StartProcessFailed)@event).CommandId);
                    Assert.Equal(_requestId, ((StartProcessFailed)@event).RequestId);
                    Assert.Equal($"Process with operationId: {operationId} already started.", ((StartProcessFailed)@event).Error.Message);
                }
                else
                {
                    Assert.Equal("ProcessFailed", @event.GetType().Name);
                    Assert.Equal(_commandId, ((ProcessFailed)@event).CommandId);
                    Assert.Equal(_requestId, ((ProcessFailed)@event).RequestId);
                    Assert.Equal($"Process with operationId: {operationId} already started.", ((ProcessFailed)@event).Error.Message);
                }

                Assert.Equal(_commandId, outboxMessage.MessageId);
            }

            _mockProcessManager.Verify(x => x.GetPrincipalIdAsync(createTenantMsg.ProcessKey, null), Times.Never());
            _mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(createTenantMsg.ProcessKey, createTenantMsg.ProcessName, It.IsAny<object>(), It.IsAny<string>()), Times.Never());
            _featureFlagService.Verify(x => x.GetFeatureFlagsAsync(createTenantMsg.ProcessKey, It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task SameMessageSentTwoTimes_SecondMessageThrowsError()
        {
            var operationId = _requestId;
            var createTenantMsg = new StartCreateTenantMsgV2(
                "name",
                "sname",
                "westeurope",
                null,
                Guid.NewGuid(),
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                null,
                operationId
                );
            using var context = Resolve<ProcesManagerDbContext>();

            await _publisher.Publish(createTenantMsg, GetValidHeaders());
            _startProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);

            var process = context.WorkflowRuns.FirstOrDefault(x => x.OperationId == operationId);
            var outboxMessage = context.OutboxMessages.SingleOrDefault();

            // Assert
            Assert.NotNull(process);
            Assert.Equal("in progress", process.Status);
            Assert.Equal(createTenantMsg.OperationId, process.OperationId);
            Assert.Equal(createTenantMsg.ProcessKey, process.WorkflowRunName);

            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), _jsonSerializerSettings);
            var subject = jsonEvent["subject"];
            Assert.Equal(OutboxMessageType.LogicApp, outboxMessage.Type);
            Assert.Equal($"api/Workflows/{operationId}", subject);
            Assert.Equal("StartProcessSucceeded", @event.GetType().Name);
            Assert.Equal(_commandId, ((StartProcessSucceeded)@event).CommandId);
            Assert.Equal(_requestId, ((StartProcessSucceeded)@event).RequestId);
            Assert.Equal(_commandId, outboxMessage.MessageId);
            _mockProcessManager.Verify(x => x.GetPrincipalIdAsync(createTenantMsg.ProcessKey, null), Times.Once());
            _mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(createTenantMsg.ProcessKey, createTenantMsg.ProcessName, It.IsAny<object>(), It.IsAny<string>()), Times.Once());
            _featureFlagService.Verify(x => x.GetFeatureFlagsAsync(createTenantMsg.ProcessKey, It.IsAny<CancellationToken>()), Times.Once());

            //if we send message with the same commandId, then second message shouldn't be processed
            //this message will be moved to error queue
            var secondCreateTenantMsg = new StartCreateTenantMsgV2(
                "name2",
                "sname2",
                "westeurope",
                null,
                Guid.NewGuid(),
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                null,
                operationId
            );
            await _publisher.Publish(secondCreateTenantMsg, GetValidHeaders());
            _startProcessMsgReceived.WaitOne(_waitTimeInMiliseconds);
            await Task.Delay(1500);

            var messageFromErrorQueue = _network.GetNextOrNull("error");
            var errorMessage = messageFromErrorQueue.Headers["rbs2-error-details"];
            Assert.Contains($"Message StartCreateTenantMsgV2 with Id {_commandId} already exists.", errorMessage);
            var jsonMessage = JObject.Parse(System.Text.Encoding.Default.GetString(messageFromErrorQueue.Body));
            var message = JsonConvert.DeserializeObject(jsonMessage.ToString(), _jsonSerializerSettings);
            Assert.Equal("StartCreateTenantMsgV2", message.GetType().Name);
            Assert.Equal(operationId, ((StartCreateTenantMsgV2)message).OperationId);
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

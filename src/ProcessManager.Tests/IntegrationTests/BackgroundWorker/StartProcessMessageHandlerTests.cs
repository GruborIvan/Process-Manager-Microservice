using Autofac;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Transport.InMem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Persistence.InMem;
using Xunit;
using Moq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using FiveDegrees.Messages.Orchestrator;
using System.Threading;
using Autofac.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using FiveDegrees.Messages.Orchestrator.Interfaces;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ProcessManager.Domain.Events;
using ProcessManager.Infrastructure.Models;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Retry.Simple;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{

    public class StartProcessMessageHandlerTests : TestFixture
    {
        private readonly Mock<IProcessService> _mockProcessManager;
        private readonly Mock<IFeatureFlagService> _featureFlagService;
        private readonly EventWaitHandle _startProcessMsgReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public StartProcessMessageHandlerTests()
        {
            _mockProcessManager = new Mock<IProcessService>();
            _mockProcessManager.Setup(service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new Process { Key = "asd", Parameters = new JObject(), StartUrl = "url" })
                .Verifiable();
            _mockProcessManager.Setup(service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
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
                });

            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<IStartProcessMsg>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<IStartProcessMsg>>>();

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
                    e.AfterMessageHandled += (bus, headers, message, context, args) => _startProcessMsgReceived.Set();
                })
                .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<StartCreateTenantMsg>().GetAwaiter().GetResult();

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        [Fact]
        public async Task SendCreateTenantCommandToServiceBus_Valid()
        {
            var operationId = _requestId;
            var correlationId = Guid.NewGuid();
            var createTenantMsg = new StartCreateTenantMsg(
                Guid.NewGuid(), 
                "name",
                "sname",
                null,
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                Guid.NewGuid(),
                null,
                operationId: operationId,
                correlationId: correlationId
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
            Assert.Equal(correlationId, ((StartProcessSucceeded)@event).CorrelationId);
            _mockProcessManager.Verify(x => x.GetPrincipalIdAsync(createTenantMsg.ProcessKey, null), Times.Once());
            _mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(createTenantMsg.ProcessKey, createTenantMsg.ProcessName, It.IsAny<object>(), null), Times.Once());
            _featureFlagService.Verify(x => x.GetFeatureFlagsAsync(createTenantMsg.ProcessKey, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task ProcessAlreadyExists_Doesnt_CreateProcess()
        {
            var operationId = _requestId;
            var correlationId = Guid.NewGuid();
            var createTenantMsg = new StartCreateTenantMsg(
                Guid.NewGuid(),
                "name",
                "sname",
                null,
                new List<EnvironmentData>(),
                new List<TenantContactPersonData>(),
                Guid.NewGuid(),
                null,
                operationId: operationId,
                correlationId: correlationId
                );
            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.Add(new WorkflowRunDbo { OperationId = _requestId });
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
                    Assert.Equal(correlationId, ((StartProcessFailed)@event).CorrelationId);
                    Assert.NotEqual(Guid.Empty, outboxMessage.MessageId);
                    Assert.Equal($"Process with operationId: {operationId} already started.", ((StartProcessFailed)@event).Error.Message);
                }
                else
                {
                    Assert.Equal("ProcessFailed", @event.GetType().Name);
                    Assert.Equal(correlationId, ((ProcessFailed)@event).CorrelationId);
                    Assert.NotEqual(Guid.Empty, outboxMessage.MessageId);
                    Assert.Equal($"Process with operationId: {operationId} already started.", ((ProcessFailed)@event).Error.Message);
                }
            }

            _mockProcessManager.Verify(x => x.GetPrincipalIdAsync(createTenantMsg.ProcessKey, null), Times.Never());
            _mockProcessManager.Verify(x => x.GetProcessWithMessageAsync(createTenantMsg.ProcessKey, createTenantMsg.ProcessName, It.IsAny<object>(), It.IsAny<string>()), Times.Never());
            _featureFlagService.Verify(x => x.GetFeatureFlagsAsync(createTenantMsg.ProcessKey, It.IsAny<CancellationToken>()), Times.Never());
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





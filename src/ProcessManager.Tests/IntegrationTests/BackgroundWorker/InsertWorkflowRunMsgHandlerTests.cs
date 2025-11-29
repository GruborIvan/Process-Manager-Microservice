using Autofac;
using Autofac.Extensions.DependencyInjection;
using FiveDegrees.Messages.ProcessManager;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json.Linq;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{

    public class InsertWorkflowRunMsgHandlerTests : TestFixture
    {
        private readonly Mock<IProcessService> _mockProcessManager;
        private readonly EventWaitHandle _insertWorkflowRunMessageReceived = new ManualResetEvent(initialState: false);
        private const int _waitTimeInMiliseconds = 10000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;

        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _commandId = Guid.NewGuid();

        public InsertWorkflowRunMsgHandlerTests()
        {
            _mockProcessManager = new Mock<IProcessService>();
            _mockProcessManager.Setup(service => service.GetProcessWithMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .ReturnsAsync(new Process { Key = "asd", Parameters = new JObject(), StartUrl = "url" })
                .Verifiable();
            _mockProcessManager.Setup(service => service.GetPrincipalIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Guid.NewGuid)
                .Verifiable();
     
            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterInstance(_mockProcessManager.Object).As<IProcessService>();
                });
     
            _host = hostBuilder.Start();
     
            var handler = Resolve<IHandleMessages<InsertWorkflowRunMsg>>();
            var failedHandler = Resolve<IHandleMessages<IFailed<InsertWorkflowRunMsg>>>();
     
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
                    e.AfterMessageHandled += (bus, headers, message, context, args) => _insertWorkflowRunMessageReceived.Set();
                })
                .Start();
     
            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();
     
            _subscriber.Subscribe<InsertWorkflowRunMsg>().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task ValidMessage_SendInsertWorkflowRunCommandToServiceBus()
        {
            // Arrange 
            var operationId = _requestId;
            var entityId = Guid.NewGuid();
            var createInsertWorkflowRunMsg = new InsertWorkflowRunMsg(
                Guid.NewGuid(),
                operationId.ToString(),
                "WorkflowRunName",
                "WorkflowRunId",
                new List<string>() {entityId.ToString()});
            using var context = Resolve<ProcesManagerDbContext>();

            var createWorkflowDbo = new WorkflowRunDbo();
            createWorkflowDbo.OperationId = operationId;

            var createRelationDbo = new RelationDbo();
            createRelationDbo.EntityId = entityId;

            context.WorkflowRuns.Add(createWorkflowDbo);
            context.Relations.Add(createRelationDbo);
            await context.SaveChangesAsync();

            // Act
            await _publisher.Publish(createInsertWorkflowRunMsg, GetValidHeaders());
            _insertWorkflowRunMessageReceived.WaitOne(_waitTimeInMiliseconds);
      
            var workflowRun = context.UnorchestratedRuns.FirstOrDefault(x => x.OperationId == operationId);

            // Assert
            Assert.NotNull(workflowRun);
        }

        [Fact]
        public async Task ValidMessage_WorkflowRunsMissing()
        {
            // Arrange 
            var operationId = _requestId;
            var createInsertWorkflowRunMsg = new InsertWorkflowRunMsg(
                Guid.NewGuid(),
                operationId.ToString(),
                "WorkflowRunName",
                "WorkflowRunId",
                null);
            using var context = Resolve<ProcesManagerDbContext>();

            // Act
            await _publisher.Publish(createInsertWorkflowRunMsg, GetValidHeaders());
            _insertWorkflowRunMessageReceived.WaitOne(_waitTimeInMiliseconds);
      
            var workflowRun = context.UnorchestratedRuns.FirstOrDefault(x => x.OperationId == operationId);

            // Assert
            Assert.Null(workflowRun);
        }

        [Fact]
        public async Task InvalidMessage_SendInsertWorkflowRunCommandToServiceBus_()
        {
            // Arrange 
            var operationId = _requestId;
            var createInsertWorkflowRunMsg = new InsertWorkflowRunMsg(Guid.Empty, _requestId.ToString(), "", "", null);
            using var context = Resolve<ProcesManagerDbContext>();

            var createWorkflowDbo = new WorkflowRunDbo();
            createWorkflowDbo.OperationId = Guid.NewGuid();

            context.WorkflowRuns.Add(createWorkflowDbo);
            await context.SaveChangesAsync();

            // Act
            await _publisher.Publish(createInsertWorkflowRunMsg, GetValidHeaders());
            _insertWorkflowRunMessageReceived.WaitOne(_waitTimeInMiliseconds);
      
            var workflowRun = context.UnorchestratedRuns.FirstOrDefault(x => x.OperationId == operationId);

            // Assert
            Assert.Null(workflowRun);
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





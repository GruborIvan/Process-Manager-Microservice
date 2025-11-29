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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public class ReportingProcessManagerHandlerTests : TestFixture
    {
        private readonly EventWaitHandle _reportingProcessManagerMsgReceived = new ManualResetEvent(initialState: false);
        private string exceptionMessage = "";
        private const int _waitTimeInMiliseconds = 20000;
        private readonly IHost _host;
        private readonly IBus _publisher;
        private readonly IBus _subscriber;

        public ReportingProcessManagerHandlerTests()
        {
            _host = hostBuilder.Start();

            var handler = Resolve<IHandleMessages<ReportingProcessManagerMsg>>();

            var publisherActivator = new BuiltinHandlerActivator();
            var subscriberActivator = new BuiltinHandlerActivator();
            subscriberActivator.Register(x => handler);

            var subscriberStore = new InMemorySubscriberStore();
            var network = new InMemNetwork();
            var queueName = "test";

            _subscriber = Configure.With(subscriberActivator)
                    .Transport(t => t.UseInMemoryTransport(network, queueName))
                    .Subscriptions(s => s.StoreInMemory(subscriberStore))
                    .Options(b => b.SimpleRetryStrategy(maxDeliveryAttempts: 1))
                    .Events(e =>
                    {
                        e.AfterMessageHandled += (bus, headers, message, context, args) =>
                        {
                            var exceptionOrNull = context.Load<Exception>();
                            if (exceptionOrNull != null)
                            {
                                exceptionMessage = exceptionOrNull.Message;
                            }
                            _reportingProcessManagerMsgReceived.Set();
                        };
                    })
                    .Start();

            _publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Subscriptions(s => s.StoreInMemory(subscriberStore))
                .Start();

            _subscriber.Subscribe<ReportingProcessManagerMsg>().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task ValidMessage_ReportingProcessManager_CreateBlobWithCorrelationIdName()
        {
            // Arrange 
            var workflowRunDbos = new List<WorkflowRunDbo>
            {
                new WorkflowRunDbo
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test",
                    Status = "started",
                    CreatedBy = "test",
                    CreatedDate = DateTime.UtcNow,
                    ChangedBy = "test",
                    ChangedDate = DateTime.UtcNow
                },
                new WorkflowRunDbo
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test2",
                    Status = "started",
                    CreatedBy = "test2",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    ChangedBy = "test2",
                    ChangedDate = DateTime.UtcNow.AddDays(-2)
                },
            };
            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.AddRange(workflowRunDbos);
            context.SaveChanges();

            var correlationId = Guid.NewGuid();
            // Act
            var reportingProcessManagerMsg = new ReportingProcessManagerMsg(correlationId, new List<ReportingProcessManagerEntities> { ReportingProcessManagerEntities.WorkflowRun }, DateTime.Now.AddDays(-1), null);

            await _publisher.Publish(reportingProcessManagerMsg);

            _reportingProcessManagerMsgReceived.WaitOne(_waitTimeInMiliseconds);

            // Assert
            Assert.Equal($"WorkflowRun/{correlationId}.json", blobData.blobName);
        }

        [Fact]
        public async Task InvalidMessage_ReportingProcessManager_InvalidDatetimeRangeException()
        {
            // Arrange 
            var workflowRunDbos = new List<WorkflowRunDbo>
            {
                new WorkflowRunDbo
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test",
                    Status = "started",
                    CreatedBy = "test",
                    CreatedDate = DateTime.UtcNow,
                    ChangedBy = "test",
                    ChangedDate = DateTime.UtcNow
                },
                new WorkflowRunDbo
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test2",
                    Status = "started",
                    CreatedBy = "test2",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    ChangedBy = "test2",
                    ChangedDate = DateTime.UtcNow.AddDays(-2)
                },
            };
            using var context = Resolve<ProcesManagerDbContext>();
            context.WorkflowRuns.AddRange(workflowRunDbos);
            context.SaveChanges();

            var correlationId = Guid.NewGuid();
            // Act
            var reportingProcessManagerMsg = new ReportingProcessManagerMsg(correlationId, new List<ReportingProcessManagerEntities> { ReportingProcessManagerEntities.WorkflowRun }, DateTime.Now.AddDays(1), null);

            await _publisher.Publish(reportingProcessManagerMsg);

            _reportingProcessManagerMsgReceived.WaitOne(_waitTimeInMiliseconds);

            // Assert
            Assert.Contains("Invalid Datetime Range", exceptionMessage);
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }
    }
}

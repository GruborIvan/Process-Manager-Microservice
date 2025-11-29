using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProcessManager.Domain.Events;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.BackgroundWorker
{
    public class StartLogicAppsWorkerTests : TestFixture
    {
        private IHost _host;
        private readonly Mock<IProcessService> _mockProcessManager;

        public StartLogicAppsWorkerTests()
        {
            _mockProcessManager = new Mock<IProcessService>();
            _mockProcessManager.Setup(service => service.StartProcessAsync(It.IsAny<Process>(), It.IsAny<Dictionary<string, string>>()))
                .Verifiable();

            hostBuilder.ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterModule(
                    new ProcessManager.BackgroundWorker.StartLogicApps.Modules.AutoMapperModule(typeof(InfrastructureModule)
                        .Assembly));
                builder.RegisterInstance(_mockProcessManager.Object).As<IProcessService>();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ProcessManager.BackgroundWorker.StartLogicApps.Worker>();
            });
        }

        [Fact]
        public async Task OneUnprocessedMessage_StartLogicAppsWorker_ProcessMessagesAndStartLogicApp()
        {
            _host = hostBuilder.Start();
            // Arrange 
            var outboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = null
            };
            var processedOutboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = DateTime.UtcNow
            };

            var workflowDbo = new WorkflowRunDbo()
            {
                OperationId = Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"),
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
            context.OutboxMessages.AddRange(outboxMessageDbo, processedOutboxMessageDbo);
            context.WorkflowRuns.Add(workflowDbo);
            context.SaveChanges();

            await Task.Delay(2500);

            //Act
            var outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();

            // Assert
            Assert.Empty(outboxMessagesLogicApp);
            Assert.Single(outboxMessagesEventGrid);
            _mockProcessManager.Verify(x => x.StartProcessAsync(It.IsAny<Process>(), It.Is<Dictionary<string, string>>(x => x["x-request-id"] == "43b2bee8-c17c-4fe9-b5ab-7290bd171c94" && x["x-command-id"] == "da5a046b-3f27-4596-8175-6324567e7956")), Times.Once);
        }

        [Fact]
        public async Task OneUnprocessedMessage_StartLogicAppsWorkerFailed_ProcessMessagesAndSaveFailedEvent()
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            _mockProcessManager.Setup(service => service.StartProcessAsync(It.IsAny<Process>(), It.IsAny<Dictionary<string, string>>()))
                .Throws(new Exception());

            hostBuilder.ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterInstance(_mockProcessManager.Object).As<IProcessService>();
            });
            _host = hostBuilder.Start();

            // Arrange 
            var outboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = null
            };
            var processedOutboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = DateTime.UtcNow
            };
            var workflowRunDbo = new WorkflowRunDbo
            {
                OperationId = Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"),
                WorkflowRunName = "TestRun",
                Status = "in progress"
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.OutboxMessages.AddRange(outboxMessageDbo, processedOutboxMessageDbo);
            context.WorkflowRuns.Add(workflowRunDbo);
            context.SaveChanges();

            await Task.Delay(2500);

            //Act
            var outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();
            var workflowRun = context.WorkflowRuns.Single(x => x.OperationId == Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"));

            // Assert
            Assert.Single(outboxMessagesLogicApp);
            Assert.Empty(outboxMessagesEventGrid);
            Assert.Equal("in progress", workflowRun.Status);

            //StartProcessAsync will fail three times, and we will not retry request again
            //We are retrying StartProcessAsync and on the third time (we have exponential backoff retries) we will send ProcessFailed event
            await Task.Delay(3500);

            //after 2 retries request should be successful

            outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var processedOutboxMessagesLogicApp = context.OutboxMessages.Single(x => x.ProcessedDate != null && x.RetryAttempt.GetValueOrDefault() > 0 && x.Type == OutboxMessageType.LogicApp);
            outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();

            Assert.Empty(outboxMessagesLogicApp);
            Assert.Single(outboxMessagesEventGrid);
            Assert.Equal(2, processedOutboxMessagesLogicApp.RetryAttempt);
            Assert.True(processedOutboxMessagesLogicApp.ProcessedDate != null);

            var outboxMessage = outboxMessagesEventGrid.Single();
            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), jsonSerializerSettings);
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal("ProcessFailed", @event.GetType().Name);
            Assert.Equal("Exception of type 'System.Exception' was thrown.", ((ProcessFailed)@event).Error.Message);
            _mockProcessManager.Verify(x => x.StartProcessAsync(It.IsAny<Process>(), It.Is<Dictionary<string, string>>(x => x["x-request-id"] == "43b2bee8-c17c-4fe9-b5ab-7290bd171c94" && x["x-command-id"] == "da5a046b-3f27-4596-8175-6324567e7956")), Times.AtLeastOnce);
        }

        [Fact]
        public async Task OneUnprocessedMessage_StartLogicAppsRetryTwoTimes_ProcessMessagesAndStartLogicApp()
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            _mockProcessManager.SetupSequence(service => service.StartProcessAsync(It.IsAny<Process>(), It.IsAny<Dictionary<string, string>>()))
                .Throws(new Exception())
                .Throws(new Exception())
                .ReturnsAsync(Guid.NewGuid().ToString());

            hostBuilder.ConfigureContainer<ContainerBuilder>(builder =>
            {
                builder.RegisterInstance(_mockProcessManager.Object).As<IProcessService>();
            });
            _host = hostBuilder.Start();

            // Arrange 
            var outboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = null
            };
            var processedOutboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = DateTime.UtcNow
            };
            var workflowRunDbo = new WorkflowRunDbo
            {
                OperationId = Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"),
                WorkflowRunName = "TestRun",
                Status = "in progress"
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.OutboxMessages.AddRange(outboxMessageDbo, processedOutboxMessageDbo);
            context.WorkflowRuns.Add(workflowRunDbo);
            context.SaveChanges();

            await Task.Delay(2500);

            //Act
            var outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();
            var workflowRun = context.WorkflowRuns.Single(x => x.OperationId == Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"));

            // Assert
            //StartProcessAsync will fail two times, but then it will be successful
            //We are retrying StartProcessAsync and on the third time (we have exponential backoff retries) we will send ProcessSuccess event

            Assert.Single(outboxMessagesLogicApp);
            Assert.Empty(outboxMessagesEventGrid);
            Assert.Equal("in progress", workflowRun.Status);

            await Task.Delay(3500);

            //after 2 retries request should be successful

            outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var processedOutboxMessagesLogicApp = context.OutboxMessages.Single(x => x.ProcessedDate != null && x.RetryAttempt.GetValueOrDefault() > 0 && x.Type == OutboxMessageType.LogicApp);
            outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();

            Assert.Empty(outboxMessagesLogicApp);
            Assert.Single(outboxMessagesEventGrid);
            Assert.Equal(2, processedOutboxMessagesLogicApp.RetryAttempt);
            Assert.True(processedOutboxMessagesLogicApp.ProcessedDate != null);

            var outboxMessage = outboxMessagesEventGrid.Single();
            var jsonEvent = JObject.Parse(outboxMessage.Data);
            var eventData = jsonEvent["data"];
            var @event = JsonConvert.DeserializeObject(eventData.ToString(), jsonSerializerSettings);
            Assert.Equal(OutboxMessageType.EventGrid, outboxMessage.Type);
            Assert.Equal("StartProcessSucceeded", @event.GetType().Name);
            _mockProcessManager.Verify(x => x.StartProcessAsync(It.IsAny<Process>(), It.Is<Dictionary<string, string>>(x => x["x-request-id"] == "43b2bee8-c17c-4fe9-b5ab-7290bd171c94" && x["x-command-id"] == "da5a046b-3f27-4596-8175-6324567e7956")), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TwoUnprocessedMessage_StartLogicAppsWorker_ProcessMessagesAndStartLogicApp()
        {
            _host = hostBuilder.Start();
            // Arrange 
            var outboxMessageDbo = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = null
            };
            var outboxMessageDbo2 = new OutboxMessageDbo
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\\\",\\\"requestId\\\":\\\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\\\",\\\"commandId\\\":\\\"da5a046b-3f27-4596-8175-6324567e7956\\\",\\\"operationId\\\":\\\"a8aa3a49-e726-4838-a585-d7b109c8eedd\\\",\\\"createdDate\\\":\\\"2021-12-03T10:28:17.1567457Z\\\"}\",\"subject\":\"api/Workflows/a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"correlationId\":\"1a3c214c-5818-463b-b4de-8d1bc8176fa3\",\"entityRelations\":null,\"personId\":\"156a60a5-6b10-4952-a7de-238dd840473f\",\"firstName\":\"Salvador\",\"lastName\":\"Dach\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Pete Fields\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"a8aa3a49-e726-4838-a585-d7b109c8eedd\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"},\"headers\":{\"x-external-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-user-id\":\"71f821ca-bf67-4b2a-938e-d0dabe3855fd\",\"x-request-id\":\"43b2bee8-c17c-4fe9-b5ab-7290bd171c94\",\"x-command-id\":\"da5a046b-3f27-4596-8175-6324567e7956\",\"rbs2-intent\":\"pub\",\"rbs2-msg-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-senttime\":\"2021-12-03T11:27:49.5112185+01:00\",\"rbs2-msg-type\":\"FiveDegrees.Messages.CdmPerson.StartCreatePersonMsg, FiveDegrees.Messages\",\"rbs2-corr-id\":\"c6948a1b-e60c-4cdf-8886-af6b80c4fd4e\",\"rbs2-corr-seq\":\"0\",\"rbs2-content-type\":\"application/json;charset=utf-8\"}}",
                ProcessedDate = null
            };

            var workflowRunDbo = new WorkflowRunDbo
            {
                OperationId = Guid.Parse("a8aa3a49-e726-4838-a585-d7b109c8eedd"),
                WorkflowRunName = "TestRun",
                Status = "in progress"
            };

            using var context = Resolve<ProcesManagerDbContext>();
            context.OutboxMessages.AddRange(outboxMessageDbo, outboxMessageDbo2);
            context.WorkflowRuns.Add(workflowRunDbo);
            context.SaveChanges();

            await Task.Delay(2500);

            //Act
            var outboxMessagesLogicApp = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.LogicApp).ToList();
            var outboxMessagesEventGrid = context.OutboxMessages.Where(x => x.ProcessedDate == null && x.Type == OutboxMessageType.EventGrid).ToList();

            // Assert
            Assert.Empty(outboxMessagesLogicApp);
            Assert.Equal(2, outboxMessagesEventGrid.Count);
            _mockProcessManager.Verify(x => x.StartProcessAsync(It.IsAny<Process>(), It.Is<Dictionary<string, string>>(x => x["x-request-id"] == "43b2bee8-c17c-4fe9-b5ab-7290bd171c94" && x["x-command-id"] == "da5a046b-3f27-4596-8175-6324567e7956")), Times.Exactly(2));
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }
    }
}

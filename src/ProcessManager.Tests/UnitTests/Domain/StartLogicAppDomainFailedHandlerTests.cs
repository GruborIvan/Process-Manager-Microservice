using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using ProcessManager.Domain.DomainEvents;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Domain
{
    public class StartLogicAppDomainFailedHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IConfiguration _configuration;

        public StartLogicAppDomainFailedHandlerTests()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _unitOfWorkMock.Setup(
                    repo => repo.OutboxRepository.AddAsync(It.IsAny<Guid>(), It.IsAny<OutboxMessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OutboxMessage())
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.WorkflowRepository.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkflowRun())
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.WorkflowRepository.Update(It.IsAny<WorkflowRun>()))
                .Verifiable();
            _unitOfWorkMock.Setup(
                    repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Verifiable();
        }

        [Fact]
        public async Task StartLogicAppDomainFailed_Retry_IncreaseRetryAttempt()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var operationId = Guid.Parse("2982919f-3bcd-4f41-b959-8060fed6117b");
            var outboxMessage = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = messageId,
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"8922087f-798e-4a1d-8a28-835a2bdcf58c\\\",\\\"requestId\\\":\\\"70e0a58a-b626-4730-a19e-c2a03aeb23cb\\\",\\\"commandId\\\":\\\"9baabf7e-9c9c-477c-83c2-1966f7c7e5a6\\\",\\\"operationId\\\":\\\"2982919f-3bcd-4f41-b959-8060fed6117b\\\",\\\"createdDate\\\":\\\"2021-10-13T12:29:21.5796703Z\\\"}\",\"subject\":\"api/Workflows/2982919f-3bcd-4f41-b959-8060fed6117b\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"2982919f-3bcd-4f41-b959-8060fed6117b\",\"correlationId\":\"8922087f-798e-4a1d-8a28-835a2bdcf58c\",\"entityRelations\":null,\"personId\":\"5ca735f1-b320-43b1-8137-acfbfc2676d6\",\"firstName\":\"Letha\",\"lastName\":\"Smith\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Bettye Crossing\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"2f93c75b-eab3-4519-b8f5-89e3d57637f9\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"2982919f-3bcd-4f41-b959-8060fed6117b\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"}}",
                ProcessedDate = null,
                RetryAttempt = null,
                NextRetryDate = null
            };

            var notification = new StartLogicAppDomainFailed(outboxMessage, "Failed message", new ErrorData("Failed message", "", "process"));
            var handler = new StartLogicAppDomainFailedHandler(_unitOfWorkMock.Object, _configuration);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.Update(It.Is<OutboxMessage>(x => x.MessageId == messageId && x.RetryAttempt.Value == 1 && x.NextRetryDate.Value.Date == DateTime.UtcNow.Date)), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/") && x.Contains("ProcessManager.Domain.Events.ProcessFailed")), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.GetAsync(It.Is<Guid>(x => x == operationId), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.Update(It.IsAny<WorkflowRun>()), Times.Never);
        }

        [Fact]
        public async Task StartLogicAppDomainFailed_DontRetry_AddFailedEvent()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var operationId = Guid.Parse("2982919f-3bcd-4f41-b959-8060fed6117b");
            var outboxMessage = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                MessageId = messageId,
                CreatedDate = DateTime.Now,
                Type = OutboxMessageType.LogicApp,
                Data = "{\"data\":\"{\\\"$type\\\":\\\"ProcessManager.Domain.Events.StartProcessSucceeded, ProcessManager.Domain\\\",\\\"processName\\\":\\\"Create-Person\\\",\\\"correlationId\\\":\\\"8922087f-798e-4a1d-8a28-835a2bdcf58c\\\",\\\"requestId\\\":\\\"70e0a58a-b626-4730-a19e-c2a03aeb23cb\\\",\\\"commandId\\\":\\\"9baabf7e-9c9c-477c-83c2-1966f7c7e5a6\\\",\\\"operationId\\\":\\\"2982919f-3bcd-4f41-b959-8060fed6117b\\\",\\\"createdDate\\\":\\\"2021-10-13T12:29:21.5796703Z\\\"}\",\"subject\":\"api/Workflows/2982919f-3bcd-4f41-b959-8060fed6117b\",\"process\":{\"parameters\":{\"startMessage\":{\"processKey\":\"Create-Person\",\"processName\":\"Create-Person\",\"operationId\":\"2982919f-3bcd-4f41-b959-8060fed6117b\",\"correlationId\":\"8922087f-798e-4a1d-8a28-835a2bdcf58c\",\"entityRelations\":null,\"personId\":\"5ca735f1-b320-43b1-8137-acfbfc2676d6\",\"firstName\":\"Letha\",\"lastName\":\"Smith\",\"initials\":null,\"middleName\":null,\"prefixLastName\":null,\"birthName\":null,\"genderReferenceId\":null,\"maritalStatusReferenceId\":null,\"birthDate\":null,\"nationalityReferenceId\":null,\"socialSecurityNumber\":null,\"socialSecurityNumberCountryReferenceId\":null,\"placeOfBirth\":null,\"countryOfBirthReferenceId\":null,\"preferredLanguageReferenceId\":null,\"salutation\":null,\"notes\":null,\"contact\":null,\"identityDocument\":null,\"knowYourCustomer\":null,\"address\":[{\"addressTypeReferenceId\":null,\"addressLine1\":\"Bettye Crossing\",\"addressLine2\":null,\"city\":null,\"postalCode\":null,\"region\":null,\"countryReferenceId\":null}],\"customField\":null},\"createdBy\":\"2f93c75b-eab3-4519-b8f5-89e3d57637f9\",\"externalId\":\"764c8e99-c059-4923-8ff6-b8102592429d\",\"operationId\":\"2982919f-3bcd-4f41-b959-8060fed6117b\",\"featureFlags\":[]},\"key\":\"Create-Person\",\"startUrl\":\"https://prod-161.westeurope.logic.azure.com:443/workflows/7fb2b0e5b1334394a262435c3a764316/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=xPgqsPE5TgB_-1fVA4ek2PHM_QrykdXkSYqD0yiSK08\"}}",
                ProcessedDate = null,
                RetryAttempt = 10,
                NextRetryDate = DateTime.UtcNow.AddMinutes(-1)
            };

            var notification = new StartLogicAppDomainFailed(outboxMessage, "Failed message", new ErrorData("Failed message", "", "process"));
            var handler = new StartLogicAppDomainFailedHandler(_unitOfWorkMock.Object, _configuration);

            // Act
            await handler.Handle(notification, default);

            // Assert
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.Update(It.Is<OutboxMessage>(x => x.MessageId == messageId && x.ProcessedDate.Value.Date == DateTime.UtcNow.Date)), Times.Once);
            _unitOfWorkMock.Verify(x =>
                x.OutboxRepository.AddAsync(It.IsAny<Guid>(), OutboxMessageType.EventGrid, It.Is<string>(x => x.Contains("api/Workflows/") && x.Contains("ProcessManager.Domain.Events.ProcessFailed")), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.GetAsync(It.Is<Guid>(x => x == operationId), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.WorkflowRepository.Update(It.IsAny<WorkflowRun>()), Times.Once);
        }
    }
}

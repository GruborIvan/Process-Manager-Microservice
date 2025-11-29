using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models.Reporting;
using ProcessManager.Infrastructure.Models;
using ProcessManager.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class ReportingServiceTests
    {
        private readonly Mock<ILogger<ReportingService>> _loggerMock = new Mock<ILogger<ReportingService>>();
        private readonly Mock<IReportingRepository> _reportingRepositoryMock = new Mock<IReportingRepository>();
        private readonly Mock<BlobServiceClient> _mockBlobServiceClient = new Mock<BlobServiceClient>();
        private readonly Mock<BlobContainerClient> _mockBlobContainerClient = new Mock<BlobContainerClient>();

        public ReportingServiceTests()
        {
            _mockBlobServiceClient.Setup(c => c.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_mockBlobContainerClient.Object);
                        _mockBlobContainerClient.Setup(
                            c => c.CreateIfNotExistsAsync(
                                It.IsAny<PublicAccessType>(),
                                It.IsAny<Dictionary<string, string>>(),
                                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                                It.IsAny<CancellationToken>())
                            );
            _mockBlobContainerClient.Setup(
                c => c.UploadBlobAsync(It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task StoreReportAsync_NoFiles_Successful()
        {
            var requestedFiles = new Dictionary<string, byte[]>() { };
            var service = new ReportingService(_reportingRepositoryMock.Object, _mockBlobServiceClient.Object, "file", _loggerMock.Object);

            await service.StoreReportAsync(Guid.NewGuid(), requestedFiles);

            _mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Never());
            _mockBlobContainerClient.Verify(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task StoreReportAsync_OneFile_Successful()
        {
            var requestedFiles = new Dictionary<string, byte[]>() { { "entitytable", Encoding.UTF8.GetBytes("{\"testData\" : \"exampe json\"}") } };
            var service = new ReportingService(_reportingRepositoryMock.Object, _mockBlobServiceClient.Object, "file", _loggerMock.Object);

            await service.StoreReportAsync(Guid.NewGuid(), requestedFiles);

            _mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Once());
            _mockBlobContainerClient.Verify(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task StoreReportAsync_TwoFiles_Successful()
        {
            var requestedFiles = new Dictionary<string, byte[]>() {
                { "entitytable", Encoding.UTF8.GetBytes("{\"testData\" : \"exampe json\"}") },
                { "entitytable2", Encoding.UTF8.GetBytes("{\"testData2\" : \"exampe json 2\"}") }
            };
            var service = new ReportingService(_reportingRepositoryMock.Object, _mockBlobServiceClient.Object, "file", _loggerMock.Object);

            await service.StoreReportAsync(Guid.NewGuid(), requestedFiles);

            _mockBlobContainerClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockBlobContainerClient.Verify(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetReportingData_Returns_ReportingData()
        {
            // Arange  
            var activityDbos = new List<ActivityReport>
            { 
                new ActivityReport
                {
                    ActivityId = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    Name = "test1",
                    Status = "in progress",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    URI = "test/test"
                },
                new ActivityReport
                {
                    ActivityId = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    Name = "test2",
                    Status = "in progress",
                    StartDate = DateTime.UtcNow.AddDays(-2),
                    EndDate = null,
                    URI = "test/test2"
                }
            };
            var workflowRunDbos = new List<WorkflowRunReport>
            {
                new WorkflowRunReport
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test",
                    Status = "started",
                    CreatedBy = "test",
                    CreatedDate = DateTime.UtcNow,
                    ChangedBy = "test",
                    ChangedDate = DateTime.UtcNow
                }
            };

            _reportingRepositoryMock
                .Setup(x => x.GetActivitiesAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(activityDbos);
            _reportingRepositoryMock
                .Setup(x => x.GetWorkflowRunsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowRunDbos);

            var repository = new ReportingService(_reportingRepositoryMock.Object, _mockBlobServiceClient.Object, "file", _loggerMock.Object);

            // Act 
            var reportingData = await repository.GetReportingDataAsync(new List<string> { "Activity", "WorkflowRun" }, DateTime.UtcNow.AddDays(-1), null);

            // Assert  
            Assert.NotNull(reportingData);
            var activityReport = reportingData.Single(x => x.Key == "Activity");
            var workflowRunReport = reportingData.Single(x => x.Key == "WorkflowRun");

            var activityList = JsonConvert.DeserializeObject<List<ActivityDbo>>(Encoding.UTF8.GetString(activityReport.Value));
            var workflowRunList = JsonConvert.DeserializeObject<List<WorkflowRunDbo>>(Encoding.UTF8.GetString(workflowRunReport.Value));
            var expectedActivitiey1 = activityDbos.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var activityObject1 = activityList.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var expectedActivitiey2 = activityDbos.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));
            var activityObject2 = activityList.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));

            var expectedworkflowRun = workflowRunDbos.Single(x => x.CreatedDate > DateTime.UtcNow.AddDays(-1));
            var workflowRunObject = workflowRunList.Single();

            Assert.Equal(2, activityList.Count);
            Assert.Single(workflowRunList);

            Assert.Equal(expectedActivitiey1.ActivityId, activityObject1.ActivityId);
            Assert.Equal(expectedActivitiey1.Name, activityObject1.Name);
            Assert.Equal(expectedActivitiey1.Status, activityObject1.Status);
            Assert.Equal(expectedActivitiey1.URI, activityObject1.URI);
            Assert.Equal(expectedActivitiey2.ActivityId, activityObject2.ActivityId);
            Assert.Equal(expectedActivitiey2.Name, activityObject2.Name);
            Assert.Equal(expectedActivitiey2.Status, activityObject2.Status);
            Assert.Equal(expectedActivitiey2.URI, activityObject2.URI);

            Assert.Equal(expectedworkflowRun.OperationId, workflowRunObject.OperationId);
            Assert.Equal(expectedworkflowRun.WorkflowRunName, workflowRunObject.WorkflowRunName);
            Assert.Equal(expectedworkflowRun.Status, workflowRunObject.Status);
            Assert.Equal(expectedworkflowRun.CreatedBy, workflowRunObject.CreatedBy);
            Assert.Equal(expectedworkflowRun.CreatedDate, workflowRunObject.CreatedDate);
            Assert.Equal(expectedworkflowRun.ChangedBy, workflowRunObject.ChangedBy);
            Assert.Equal(expectedworkflowRun.ChangedDate, workflowRunObject.ChangedDate);
        }

        [Fact]
        public async Task GetReportingDataForActivity_Returns_OnlyActivities()
        {
            // Arange  
            var activityDbos = new List<ActivityReport>
            {
                new ActivityReport
                {
                    ActivityId = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    Name = "test1",
                    Status = "in progress",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    URI = "test/test"
                },
                new ActivityReport
                {
                    ActivityId = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    Name = "test2",
                    Status = "in progress",
                    StartDate = DateTime.UtcNow.AddDays(-2),
                    EndDate = null,
                    URI = "test/test2"
                }
            };
            var workflowRunDbos = new List<WorkflowRunReport>
            {
                new WorkflowRunReport
                {
                    OperationId = Guid.NewGuid(),
                    WorkflowRunName = "test",
                    Status = "started",
                    CreatedBy = "test",
                    CreatedDate = DateTime.UtcNow,
                    ChangedBy = "test",
                    ChangedDate = DateTime.UtcNow
                }
            };

            _reportingRepositoryMock
                .Setup(x => x.GetActivitiesAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(activityDbos);
            _reportingRepositoryMock
                .Setup(x => x.GetWorkflowRunsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflowRunDbos);

            var repository = new ReportingService(_reportingRepositoryMock.Object, _mockBlobServiceClient.Object, "file", _loggerMock.Object);

            // Act 
            var reportingData = await repository.GetReportingDataAsync(new List<string> { "Activity" }, DateTime.UtcNow.AddDays(-1), null);

            // Assert  
            Assert.NotNull(reportingData);
            var activityReport = reportingData.Single(x => x.Key == "Activity");
            var workflowRunReport = reportingData.SingleOrDefault(x => x.Key == "WorkflowRun");

            var activityList = JsonConvert.DeserializeObject<List<ActivityDbo>>(Encoding.UTF8.GetString(activityReport.Value));
            var expectedActivity1 = activityDbos.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var activityObject1 = activityList.Single(x => x.StartDate > DateTime.UtcNow.AddDays(-1));
            var expectedActivity2 = activityDbos.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));
            var activityObject2 = activityList.Single(x => x.StartDate < DateTime.UtcNow.AddDays(-1));

            Assert.Equal(2, activityList.Count);
            Assert.Null(workflowRunReport.Key);
            Assert.Null(workflowRunReport.Value);

            Assert.Equal(expectedActivity1.ActivityId, activityObject1.ActivityId);
            Assert.Equal(expectedActivity1.Name, activityObject1.Name);
            Assert.Equal(expectedActivity1.Status, activityObject1.Status);
            Assert.Equal(expectedActivity1.URI, activityObject1.URI);
            Assert.Equal(expectedActivity2.ActivityId, activityObject2.ActivityId);
            Assert.Equal(expectedActivity2.Name, activityObject2.Name);
            Assert.Equal(expectedActivity2.Status, activityObject2.Status);
            Assert.Equal(expectedActivity2.URI, activityObject2.URI);
        }
    }
}

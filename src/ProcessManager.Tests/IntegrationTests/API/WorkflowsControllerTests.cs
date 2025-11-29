using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.TestHost;
using ProcessManager.Infrastructure.Models;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using ProcessManager.API.Models;
using System.Net;
using Newtonsoft.Json;

namespace ProcessManager.Tests.IntegrationTests.API
{
    public class WorkflowsControllerTests : TestFixture
    {
        private const string _apiUrl = "api/Workflows";

        private IHost _host;
        private HttpClient _client;

        [Fact]
        public async Task GetOperationStatusByOperationId_Returns_Workflow()
        {
            // Arrange
            _host = hostBuilder.Start();
            _client = _host.GetTestClient();

            var operationId = Guid.NewGuid();
            var expectedDbo = new WorkflowRunDbo
            {
                OperationId = operationId,
                WorkflowRunName = "test",
                Status = "started",
                CreatedBy = "test",
                CreatedDate = DateTime.UtcNow,
                ChangedBy = "test",
                ChangedDate = DateTime.UtcNow
            };

            using var context = Resolve<ProcesManagerDbContext>();
            await context.WorkflowRuns.AddAsync(expectedDbo);
            await context.SaveChangesAsync();

            // Act
            using var response = await _client.GetAsync($"{_apiUrl}/{operationId}");

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var workflow = JsonConvert.DeserializeObject<WorkflowRunDto>(result);

            Assert.NotNull(result);
            Assert.Equal(expectedDbo.ChangedDate, workflow.LastActionDateTime);
            Assert.Equal(expectedDbo.Status, workflow.Status);
            Assert.Equal(expectedDbo.CreatedDate, workflow.CreatedDateTime);
        }

        [Fact]
        public async Task GetOperationStatusByOperationId_Returns_NotFound()
        {
            // Arrange
            _host = hostBuilder.Start();
            _client = _host.GetTestClient();

            // Act
            const string operationId = "7df152f8-e967-4a07-9406-441392216158";
            using var response = await _client.GetAsync($"{_apiUrl}/{operationId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessManager.API.Controllers;
using ProcessManager.API.Models;
using ProcessManager.Domain.Exceptions;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Api
{
    public class WorkflowsControllerTests
    {
        private static readonly ILogger<WorkflowsController> _mockLoggerObject =
            new Mock<ILogger<WorkflowsController>>().Object;

        private readonly Mock<IMapper> _mockMapper = new Mock<IMapper>();
        private readonly Mock<IWorkflowRepository> _mockRepository = new Mock<IWorkflowRepository>();

        [Fact]
        public async Task GetOperationStatusByOperationId_Returns_Workflow()
        {
            // Arrange
            var operationId = Guid.Parse("21fc38b3-da2e-4c0e-a058-3b4e170e592f");
            var expectedDto = new WorkflowRunDto
            {
                Status = "started",
                CreatedDateTime = DateTime.UtcNow,
                LastActionDateTime = DateTime.UtcNow
            };

            _mockMapper.Setup(mapper => mapper.Map<WorkflowRunDto>(It.IsAny<WorkflowRun>()))
                .Returns(expectedDto);

            var controller = new WorkflowsController(_mockLoggerObject, _mockMapper.Object, _mockRepository.Object);

            // Act
            var actionResult = await controller.GetOperationStatusByOperationId(operationId);

            // Assert
            Assert.NotNull(actionResult);

            var workflow = actionResult.Value;

            Assert.NotNull(workflow);
            Assert.IsType<WorkflowRunDto>(workflow);
            Assert.Equal(expectedDto.Status, workflow.Status);
            Assert.Equal(expectedDto.CreatedDateTime, workflow.CreatedDateTime);
            Assert.Equal(expectedDto.LastActionDateTime, workflow.LastActionDateTime);
        }

        [Fact]
        public async Task GetOperationStatusByOperationId_Returns_NotFound()
        {
            // Arrange
            var nonExistingOperationId = Guid.Parse("21fc38b3-da2e-4c0e-a058-3b4e170e592f");

            _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Throws(new WorkflowNotFoundException());

            var controller = new WorkflowsController(_mockLoggerObject, _mockMapper.Object, _mockRepository.Object);

            // Act
            var actionResult = await controller.GetOperationStatusByOperationId(nonExistingOperationId);

            // Assert
            Assert.NotNull(actionResult);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetOperationStatusByOperationId_Returns_Status500InternalServerError()
        {
            // Arrange
            var nonExistingOperationId = Guid.Parse("21fc38b3-da2e-4c0e-a058-3b4e170e592f");

            _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            var controller = new WorkflowsController(_mockLoggerObject, _mockMapper.Object, _mockRepository.Object);

            // Act
            var actionResult = await controller.GetOperationStatusByOperationId(nonExistingOperationId);
            var result = (StatusCodeResult)actionResult.Result;

            // Assert
            Assert.NotNull(actionResult);
            Assert.IsType<StatusCodeResult>(actionResult.Result);
            Assert.Equal(500, result.StatusCode);
        }
    }
}

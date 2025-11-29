using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ProcessManager.API.Controllers;
using ProcessManager.API.Models.HealthCheck;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Api
{
    public class HealthControllerTests
    {
        [Fact]
        public void Return_Succeseful_Liveness_Health_Status()
        {
            // Arrange
            var healthCheckService  = new Mock<HealthCheckService>();
            var healthController = new HealthController(healthCheckService.Object);

            // Act
            var result = healthController.Liveness();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<HealthCheckActionResult>(result);
        }

        [Fact]
        public void Return_Succeseful_Readiness_Status()
        {
            // Arrange
            var healthCheckService  = new Mock<HealthCheckService>();
            var healthController = new HealthController(healthCheckService.Object);

            // Act
            var result = healthController.Readiness();

            // Assert
            Assert.NotNull(result);
             Assert.IsType<HealthCheckActionResult>(result);
        }
    }
}

using ProcessManager.BackgroundWorker.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProcessManager.Tests.UnitTests.BackgroundWorker.HealthChecks
{
    public class HealthCheckPublisherTests
    {
        private readonly Mock<ILogger<HealthCheckPublisher>> _mockLogger =
            new Mock<ILogger<HealthCheckPublisher>>();

        [Fact]
        public async void IsHealthy()
        {
            //Arrange
            var healthReportEntry = new HealthReportEntry(HealthStatus.Healthy, null, new TimeSpan(), null, null);
            var healthReportEntries = new Dictionary<string, HealthReportEntry> { { "healthyKey", healthReportEntry } };

            HealthReport healthReport = new HealthReport(healthReportEntries, new TimeSpan());

            HealthCheckPublisher publisher = new HealthCheckPublisher(_mockLogger.Object);

            //Act
            await publisher.PublishAsync(healthReport, default);

            // Assert
            _mockLogger.VerifyAll();
        }

        [Fact]
        public async void IsUnhealthy()
        {
            //Arrange
            var healthReportEntry = new HealthReportEntry(HealthStatus.Unhealthy, null, new TimeSpan(), null, null);
            var healthReportEntries = new Dictionary<string, HealthReportEntry> { { "healthyKey", healthReportEntry } };

            HealthReport healthReport = new HealthReport(healthReportEntries, new TimeSpan());

            HealthCheckPublisher publisher = new HealthCheckPublisher(_mockLogger.Object);

            //Act
            await publisher.PublishAsync(healthReport, default);

            // Assert
            _mockLogger.VerifyAll();
        }
    }
}

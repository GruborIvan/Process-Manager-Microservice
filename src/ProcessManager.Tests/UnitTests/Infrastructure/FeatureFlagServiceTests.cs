using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;
using Moq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Services;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class FeatureFlagServiceTests
    {
        private readonly Mock<IFeatureManager> _featureManagerMock;
        private readonly Mock<ICacheRepository> _cacheRepositoryMock;

        public async IAsyncEnumerable<string> GetFeatureFlagValues()
        {
            yield return "Logic-App";
            yield return "Logic-App-Test";

            await Task.CompletedTask;
        }

        public FeatureFlagServiceTests()
        {
            _featureManagerMock = new Mock<IFeatureManager>();
            _cacheRepositoryMock = new Mock<ICacheRepository>();
        }

        [Fact]
        public async Task GetFeatureFlagsAsync_FeatureFlagsAreDisabled_EmptyList()
        {
            _featureManagerMock
                .Setup(x =>
                    x.GetFeatureNamesAsync())
                .Returns(GetFeatureFlagValues);

            _featureManagerMock
                .Setup(x =>
                    x.IsEnabledAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var featureFlagService = new FeatureFlagService(_featureManagerMock.Object, _cacheRepositoryMock.Object);

            var featureFlags = await featureFlagService.GetFeatureFlagsAsync("Logic-App", It.IsAny<CancellationToken>());

            Assert.Empty(featureFlags);
        }
    }
}

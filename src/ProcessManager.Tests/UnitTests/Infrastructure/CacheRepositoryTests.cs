using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Infrastructure.Repository;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class CacheRepositoryTests
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly Mock<IFeatureFlagService> _mockFeatureFlagService;

        public CacheRepositoryTests()
        {
            _mockFeatureFlagService = new Mock<IFeatureFlagService>();
            _mockFeatureFlagService.Setup(pr => pr.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    "FeatureFlag1",
                    "FeatureFlag2",
                    "FeatureFlag3"
                });

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var provider = new ServiceCollection()
                .AddMemoryCache()
                .BuildServiceProvider();

            _cache = provider.GetService<IMemoryCache>();
        }

        [Fact]
        public async Task GetFeatureFlagsAsync_CacheIsEmpty_GetFeatureFlagsAsyncIsCalledAndCacheHasValue()
        {
            var cacheKey = $"FeatureFlag";

            var hasCacheValue = _cache.TryGetValue(cacheKey, out List<string> featureFlags);
            Assert.False(hasCacheValue);
            Assert.Null(featureFlags);

            var cacheRepository = new CacheRepository(_cache, _configuration);
            await cacheRepository.GetOrSetValueAsync(async () => await _mockFeatureFlagService.Object.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), cacheKey);

            _mockFeatureFlagService.Verify(x => x.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            hasCacheValue = _cache.TryGetValue(cacheKey, out featureFlags);
            Assert.True(hasCacheValue);
            Assert.NotNull(featureFlags);
        }

        [Fact]
        public async Task GetFeatureFlagsAsync_CacheIsEmpty_ReturnsEmptyCollection_CacheShouldBeEmpty()
        {
            var cacheKey = $"FeatureFlag";

            _mockFeatureFlagService
                .Setup(m => m.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            var hasCacheValue = _cache.TryGetValue(cacheKey, out List<string> featureFlags);
            Assert.False(hasCacheValue);
            Assert.Null(featureFlags);

            var cacheRepository = new CacheRepository(_cache, _configuration);
            await cacheRepository.GetOrSetValueAsync(async () => await _mockFeatureFlagService.Object.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), cacheKey);

            _mockFeatureFlagService.Verify(x => x.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            hasCacheValue = _cache.TryGetValue(cacheKey, out featureFlags);
            Assert.False(hasCacheValue);
            Assert.Null(featureFlags);
        }

        [Fact]
        public async Task GetFeatureFlagsAsync_CacheIsNotEmpty_Returns_FeatureFlagsFromCache()
        {
            var cacheKey = $"FeatureFlag";

            var cacheExpirationOptions = new MemoryCacheEntryOptions();
            cacheExpirationOptions.AbsoluteExpiration = DateTime.Now.AddSeconds(_configuration.GetValue<int>("ProcessManagerConfiguration:DefaultAbsoluteExpirationInSeconds"));
            cacheExpirationOptions.Priority = CacheItemPriority.Normal;

            var featureFlagsFromCache = new List<string>
            {
                "FeatureFlagFromCache"
            };

            _cache.Set(cacheKey, featureFlagsFromCache, cacheExpirationOptions);

            var hasCacheValue = _cache.TryGetValue(cacheKey, out List<string> featureFlags);
            Assert.True(hasCacheValue);
            Assert.NotNull(featureFlags);

            var cacheRepository = new CacheRepository(_cache, _configuration);
            await cacheRepository.GetOrSetValueAsync(async () => await _mockFeatureFlagService.Object.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), cacheKey);

            _mockFeatureFlagService.Verify(x => x.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

            hasCacheValue = _cache.TryGetValue(cacheKey, out featureFlags);
            Assert.True(hasCacheValue);
            Assert.NotNull(featureFlags);
        }

        [Fact]
        public async Task GetFeatureFlagsAsync_FillCacheAndWaitToExpireCache_CacheIsEmpty()
        {
            var cacheTimeoutInMs = 1000;
            var cacheKey = "FeatureFlag";

            var hasCacheValue = _cache.TryGetValue(cacheKey, out List<string> featureFlags);
            Assert.False(hasCacheValue);
            Assert.Null(featureFlags);

            var cacheRepository = new CacheRepository(_cache, _configuration);
            await cacheRepository.GetOrSetValueAsync(async () => await _mockFeatureFlagService.Object.GetFeatureFlagsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), cacheKey, DateTime.Now.AddMilliseconds(cacheTimeoutInMs));

            hasCacheValue = _cache.TryGetValue(cacheKey, out featureFlags);
            Assert.True(hasCacheValue);
            Assert.NotNull(featureFlags);

            await Task.Delay(cacheTimeoutInMs + 500);

            hasCacheValue = _cache.TryGetValue(cacheKey, out featureFlags);
            Assert.False(hasCacheValue);
            Assert.Null(featureFlags);
        }
    }
}

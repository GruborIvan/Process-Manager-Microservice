using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;
using ProcessManager.Domain.Interfaces;

namespace ProcessManager.Infrastructure.Services
{
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly IFeatureManager _featureManager;
        private readonly ICacheRepository _cacheRepository;

        public FeatureFlagService(IFeatureManager featureManager, ICacheRepository cacheRepository)
        {
            _featureManager = featureManager;
            _cacheRepository = cacheRepository;
        }

        public async Task<List<string>> GetFeatureFlagsAsync(string processKey, CancellationToken cancellationToken = default)
        {
            var featureNames = new List<string>();

            await _cacheRepository.GetOrSetValueAsync(async () =>
            {
                await foreach (string featureName in _featureManager.GetFeatureNamesAsync().WithCancellation(cancellationToken))
                {
                    if (featureName.Contains(processKey) && !featureName.Contains($"{processKey}-") && await _featureManager.IsEnabledAsync(featureName))
                    {
                        featureNames.Add(featureName);
                    }
                }

                if (featureNames.Count == 0)
                {
                    return new List<string> { "null" };
                }

                return featureNames;
            }, $"FeatureFlag-{processKey}");

            return featureNames;
        }
    }
}

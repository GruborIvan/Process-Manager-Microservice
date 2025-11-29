using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface IFeatureFlagService
    {
        Task<List<string>> GetFeatureFlagsAsync(string processKey, CancellationToken cancellationToken = default);
    }
}

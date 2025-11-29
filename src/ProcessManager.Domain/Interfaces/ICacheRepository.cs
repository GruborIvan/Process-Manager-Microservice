using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Domain.Interfaces
{
    public interface ICacheRepository
    {
        public Task<TResult> GetOrSetValueAsync<TResult>(Func<Task<TResult>> function, string cacheKey, DateTime? absoluteExpiration = null, TimeSpan? slidingExpiration = null, CancellationToken ct = default);
    }
}

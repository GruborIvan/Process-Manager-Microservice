using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using ProcessManager.Domain.Interfaces;

namespace ProcessManager.Infrastructure.Repository
{
    public class CacheRepository : ICacheRepository
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public CacheRepository(
            IMemoryCache memoryCache,
            IConfiguration configuration
            )
        {
            _cache = memoryCache;
            _configuration = configuration;
        }

        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(100);

        public async Task<TResult> GetOrSetValueAsync<TResult>(Func<Task<TResult>> function, string cacheKey, DateTime? absoluteExpiration = null, TimeSpan? slidingExpiration = null, CancellationToken ct = default)
        {
            TResult result;

            if (_cache.TryGetValue(cacheKey, out result))
            {
                return result;
            }

            await SemaphoreSlim.WaitAsync(ct);
            try
            {
                if (_cache.TryGetValue(cacheKey, out result))
                {
                    return result;
                }

                result = await function.Invoke();

                if (result is ICollection && ((ICollection)result).Count > 0 || !(result is ICollection) && result != null)
                {
                    var cacheExpirationOptions = new MemoryCacheEntryOptions()
                    {
                        AbsoluteExpiration = absoluteExpiration ?? DateTime.Now.AddSeconds(_configuration.GetSection("ProcessManagerConfiguration").GetValue<int>("DefaultAbsoluteExpirationInSeconds")),
                        SlidingExpiration = slidingExpiration ?? TimeSpan.FromSeconds(_configuration.GetSection("ProcessManagerConfiguration").GetValue<int>("DefaultSlidingExpirationInSeconds"))
                    };

                    _cache.Set(cacheKey, result, cacheExpirationOptions);
                }

                return result;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
    }

}

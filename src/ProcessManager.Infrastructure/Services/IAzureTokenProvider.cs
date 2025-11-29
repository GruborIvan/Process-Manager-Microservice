using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public interface IAzureTokenProvider
    {
        Task<AuthenticationHeaderValue> GetAuthorizationTokenAsync(string resource);
    }
}
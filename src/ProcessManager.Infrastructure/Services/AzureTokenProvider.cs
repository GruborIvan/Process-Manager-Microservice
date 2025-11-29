using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class AzureTokenProvider : IAzureTokenProvider
    {
        private readonly AzureServiceTokenProvider _azureServiceTokenProvider;

        public AzureTokenProvider(AzureServiceTokenProvider azureServiceTokenProvider)
        {
            _azureServiceTokenProvider = azureServiceTokenProvider;
        }

        public async Task<AuthenticationHeaderValue> GetAuthorizationTokenAsync(string resource)
        {
            var accessToken = await _azureServiceTokenProvider.GetAccessTokenAsync(resource);

            return new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}

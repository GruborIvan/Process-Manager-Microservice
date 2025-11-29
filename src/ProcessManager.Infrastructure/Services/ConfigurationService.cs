using Microsoft.Extensions.Logging;
using ProcessManager.Domain.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private const string EmptyConfigurationArrayJson = "[]";
        private const string SubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _factory;
        private readonly IAzureTokenProvider _tokenProvider;

        private readonly string _configurationUri;
        private readonly string _environmentId;
        private readonly string _configurationManagerBaseUrl;
        private readonly string _apimBaseUrl;
        private readonly string _apiSubscriptionKey;
        private readonly string _authScope;

        public ConfigurationService(ILogger<ConfigurationService> logger,
            IHttpClientFactory factory,
            IAzureTokenProvider tokenProvider,
            string environmentId,
            string configurationUri,
            string configurationManagerBaseUrl,
            string apimBaseUrl,
            string apiSubscriptionKey,
            string authScope)
        {
            _logger = logger;
            _environmentId = environmentId;
            _configurationUri = configurationUri;
            _configurationManagerBaseUrl = configurationManagerBaseUrl;
            _factory = factory;
            _tokenProvider = tokenProvider;
            _apimBaseUrl = apimBaseUrl;
            _apiSubscriptionKey = apiSubscriptionKey;
            _authScope = authScope;
        }

        public async Task<string> GetConfigurationAsync(string processId)
        {
            var httpClient = await CreateHttpClientAsync();
            if (httpClient.BaseAddress == null)
            {
                return EmptyConfigurationArrayJson;
            }

            _logger.LogInformation($"Fetching configuration for ProcessId: {processId}" +
                $" and EnvironmentId: {_environmentId}...");
            try
            {
                var response = await httpClient.GetAsync($"/{_configurationManagerBaseUrl}/{_configurationUri}?environmentId={_environmentId}&workflowId={processId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return EmptyConfigurationArrayJson;
                }

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while fetching configuration for ProcessId: {processId}" +
                    $" and EnvironmentId: {_environmentId}...\n" +
                    $"Message: {e.Message}");
                throw;
            }
        }

        private async Task<HttpClient> CreateHttpClientAsync()
        {
            var httpClient = _factory.CreateClient();
            httpClient.BaseAddress = string.IsNullOrEmpty(_apimBaseUrl) ? null : new Uri(_apimBaseUrl);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add(SubscriptionKeyHeader, _apiSubscriptionKey);
            httpClient.DefaultRequestHeaders.Authorization = await _tokenProvider.GetAuthorizationTokenAsync(_authScope);
            return httpClient;
        }
    }
}

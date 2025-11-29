using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProcessManager.Infrastructure.Services;

namespace ProcessManager.BackgroundWorker.HealthChecks
{
    public class ServiceHealthChecker : IHealthCheck
    {
        private const string _subscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        private readonly string _serviceName;
        private readonly string _serviceUrl;
        private readonly string _apimKey;
        private readonly string _authScope;

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _options;
        private readonly IAzureTokenProvider _azureTokenProvider;

        public ServiceHealthChecker(string serviceName, string serviceUrl, string apimKey, string authScope, IAzureTokenProvider azureTokenProvider, IHttpClientFactory httpClientFactory)
        {
            _azureTokenProvider = azureTokenProvider;
            _authScope = string.IsNullOrWhiteSpace(authScope) ? throw new ArgumentNullException(nameof(authScope)) : authScope;
            _serviceName = string.IsNullOrWhiteSpace(serviceName) ? throw new ArgumentNullException(nameof(serviceName)) : serviceName;
            _serviceUrl = string.IsNullOrWhiteSpace(serviceUrl) ? throw new ArgumentNullException(nameof(serviceUrl)) : serviceUrl;
            _apimKey = string.IsNullOrWhiteSpace(apimKey) ? throw new ArgumentNullException(nameof(apimKey)) : apimKey;

            _httpClient = httpClientFactory.CreateClient();
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add(_subscriptionKeyHeader, _apimKey);
                _httpClient.DefaultRequestHeaders.Authorization = await _azureTokenProvider.GetAuthorizationTokenAsync(_authScope);
                var endpoint = $"{_serviceUrl}/health/liveness";
                var responseBody = await _httpClient.GetAsync(endpoint, cancellationToken);
                var json = await responseBody.Content.ReadAsStringAsync();
                var livenessResponseObject = JsonSerializer.Deserialize<LivenessCheckResponse>(json);

                if (!"Healthy".Equals(livenessResponseObject.OverallStatus))
                {
                    return HealthCheckResult.Unhealthy($"{_serviceName} is not healthy");
                }

                endpoint = $"{_serviceUrl}/health/readiness";
                responseBody = await _httpClient.GetAsync(endpoint, cancellationToken);
                json = await responseBody.Content.ReadAsStringAsync();
                var readinessResponseObject = JsonSerializer.Deserialize<HealthCheckResponse>(json, _options);

                if (!"Healthy".Equals(readinessResponseObject.Status))
                {
                    return HealthCheckResult.Unhealthy($"{_serviceName} is not healthy");
                }

                return HealthCheckResult.Healthy($"{_serviceName} is accessible");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy($"{_serviceName} check resulted in an exception", e);
            }
        }
    }

    public class HealthCheckResponse
    {
        public string Status { get; set; }
        public string TotalDuration { get; set; }
        public object Entries { get; set; }
    }

    public class LivenessCheckResponse
    {
        public string OverallStatus { get; set; }
        public string TotalChecksDuration { get; set; }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.Services
{
    public class LogicAppProcessService : IProcessService
    {
        private readonly HttpClient _httpClient;
        private readonly IAzureTokenProvider _tokenProvider;
        private readonly IConfigurationService _configurationService;
        private readonly ICacheRepository _cacheRepository;
        private readonly string _logicAppDefinitionsUrl = @"https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}/triggers/manual/listCallbackUrl?api-version=2016-06-01";
        private readonly string _getWorkflowUrl = "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}?api-version=2016-06-01";
        private readonly string _subscription;
        private readonly string _resourceGroup;
        private readonly string _authScope = "https://management.azure.com/";

        public LogicAppProcessService(string subscription,
            string resourceGroup,
            IHttpClientFactory clientFactory,
            IAzureTokenProvider tokenProvider,
            IConfigurationService configurationService,
            ICacheRepository cacheRepository)
        {
            _httpClient = clientFactory.CreateClient();
            _subscription = subscription;
            _resourceGroup = resourceGroup;
            _tokenProvider = tokenProvider;
            _configurationService = configurationService;
            _cacheRepository = cacheRepository;
        }

        public async Task<Process> GetProcessWithMessageAsync(string processKey, string processName, object message, string environmentName)
        {
            if (string.IsNullOrEmpty(processKey))
            {
                throw new ArgumentException("Process key not valid", nameof(processKey));
            }
            if (string.IsNullOrEmpty(processName))
            {
                throw new ArgumentException("Process name not valid", nameof(processName));
            }

            _ = message ?? throw new ArgumentException("Start Parameters missing", nameof(message));

            var inputParameters = CreateInputParameters(message);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = await _tokenProvider.GetAuthorizationTokenAsync(_authScope);
            var url = await _cacheRepository.GetOrSetValueAsync(async () =>
            {
                var httpResult = await _httpClient.PostAsync(FormatGetDefinitionUrl(processName, environmentName), null);
                httpResult.EnsureSuccessStatusCode();
                var body = await httpResult.Content.ReadAsStringAsync();
                var success = TryGetUrl(body, out var resultUrl);
                if (!success)
                {
                    throw new InvalidOperationException($"Process with {processName} key doesn't exist.");
                }

                return resultUrl;
            }, $"{processKey}-{environmentName}");

            return new Process
            {
                Key = processKey,
                Parameters = inputParameters,
                StartUrl = url
            };
        }

        public async Task<Guid> GetPrincipalIdAsync(string processName, string environmentName)
        {
            if (string.IsNullOrEmpty(processName))
            {
                throw new ArgumentException("Process name not valid", nameof(processName));
            }

            var principalId = await _cacheRepository.GetOrSetValueAsync(async () =>
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = await _tokenProvider.GetAuthorizationTokenAsync(_authScope);
                var result = await _httpClient.GetAsync(string.Format(_getWorkflowUrl, _subscription, GetResourceGroup(environmentName), processName));

                result.EnsureSuccessStatusCode();

                var body = await result.Content.ReadAsStringAsync();

                var success = GetPrincipalId(body, out Guid principalId);

                if (!success)
                {
                    throw new InvalidOperationException($"Identity with {processName} key doesn't exist.");
                }

                return principalId;
            },$"PrincipalId-{processName}-{environmentName}");

            return principalId;
        }

        private JObject CreateInputParameters(object value)
            => JObject.FromObject(value);

        private bool TryGetUrl(string response, out string url)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(response);
                url = json.value;
                return Uri.IsWellFormedUriString(url, UriKind.Absolute);
            }
            catch
            {
                url = string.Empty;
                return false;
            }
        }

        private string GetResourceGroup(string environmentName)
        {
            if (!_resourceGroup.Contains("platform-rg") && !string.IsNullOrEmpty(environmentName))
            {
                var resourceGroupParts = _resourceGroup.Split('-');
                resourceGroupParts[^2] = environmentName;
                return string.Join("-", resourceGroupParts);
            }

            return _resourceGroup;
        }

        private bool GetPrincipalId(string response, out Guid principalId)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject(response);
                return Guid.TryParse(json.identity.principalId.ToString(), out principalId);
            }
            catch
            {
                principalId = Guid.Empty;
                return false;
            }
        }

        private string FormatGetDefinitionUrl(string processName, string environmentName)
        {
            return string.Format(_logicAppDefinitionsUrl, _subscription, GetResourceGroup(environmentName), processName);
        }

        public async Task<string> StartProcessAsync(Process process, Dictionary<string,string> headers)
        {
            _ = process ?? throw new ArgumentNullException(nameof(process));

            //Add the parameters
            var json = new JObject
            {
                ["Parameters"] = process.Parameters
            };

            if (!_resourceGroup.Contains("platform-rg"))
            {
                var config = JsonConvert
                .DeserializeObject<IEnumerable<ConfigurationParameter>>
                (await _configurationService.GetConfigurationAsync(process.Key));

                //Add the configuration
                foreach (var c in config)
                {
                    json[c.SettingKey] = c.ValueType switch
                    {
                        ValueType.Bool => c.BoolValue,
                        ValueType.Guid => c.GuidValue,
                        ValueType.Int => c.IntValue,
                        ValueType.String => c.StringValue,
                        _ => throw new ArgumentException("Invalid enum value", nameof(c.ValueType)),
                    };
                }
            }

            //Set the content
            var content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");

            //Clear the headers from the previous call to fetch Authorization token
            _httpClient.DefaultRequestHeaders.Clear();

            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var response = await _httpClient.PostAsync(process.StartUrl, content);

            var workflowHeaders = response.Headers.TryGetValues("x-ms-workflow-run-id", out IEnumerable<string> values);

            var msWorkflowRunId = workflowHeaders ? values.First() : String.Empty;

            response.EnsureSuccessStatusCode();

            return msWorkflowRunId;
        }

        public class ConfigurationParameter
        {
            public Guid? WorkflowSettingId { get; set; }
            public string? WorkflowId { get; set; }
            public string? WorkflowName { get; set; }
            public string? SettingKey { get; set; }
            public ValueType? ValueType { get; set; }
            public Guid? EnvironmentId { get; set; }
            public bool? BoolValue { get; set; }
            public string? StringValue { get; set; }
            public int? IntValue { get; set; }
            public Guid? GuidValue { get; set; }
        }

        public class SecretsTemplate
        {
            public Guid DocumentTemplateSettingId { get; set; }
            public Guid EnvironmentId { get; set; }
            public int SettingLevel { get; set; }
            public string DevelopersKey { get; set; }
            public string Secret { get; set; }
            public string DocumentTemplateUrl { get; set; }
        }

        public enum ValueType
        {
            Int = 1,
            Bool = 2,
            String = 3,
            Guid = 4
        }
    }
}

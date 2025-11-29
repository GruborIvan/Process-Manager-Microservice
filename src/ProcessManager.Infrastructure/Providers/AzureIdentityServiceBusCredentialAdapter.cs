using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.ServiceBus.Primitives;

namespace ProcessManager.Infrastructure.Providers
{
    public class AzureIdentityServiceBusCredentialAdapter : ITokenProvider
    {
        private readonly TokenCredential tokenCredential;

        public AzureIdentityServiceBusCredentialAdapter() : this(new DefaultAzureCredential())
        {
        }

        public AzureIdentityServiceBusCredentialAdapter(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential;
        }

        public async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter((int)timeout.TotalMilliseconds);

            var token = await new AzureIdentityTokenProvider(
                    tokenCredential,
                    new string[] { "https://servicebus.azure.net/.default" })
                .GetTokenAsync(cts.Token);

            var appliesToUri = new Uri(appliesTo);

            return new JsonSecurityToken(token.Token, appliesToUri.Host);
        }
    }
}

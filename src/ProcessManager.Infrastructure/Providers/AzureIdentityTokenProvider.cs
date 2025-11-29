using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Rest;

namespace ProcessManager.Infrastructure.Providers
{
    public class AzureIdentityTokenProvider : ITokenProvider
    {
        private AccessToken? _accessToken;
        private static readonly TimeSpan ExpirationThreshold = TimeSpan.FromMinutes(5);
        private readonly string[] _scopes;

        private readonly TokenCredential _tokenCredential;

        public AzureIdentityTokenProvider(string[] scopes = null) : this(new DefaultAzureCredential(), scopes)
        {
        }

        public AzureIdentityTokenProvider(TokenCredential tokenCredential, string[] scopes = null)
        {
            if (scopes == null || scopes.Length == 0)
            {
                scopes = new string[] { "https://management.azure.com/.default" };
            }

            _scopes = scopes;
            _tokenCredential = tokenCredential;
        }

        public virtual async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            var accessToken = await GetTokenAsync(cancellationToken);
            return new AuthenticationHeaderValue("Bearer", accessToken.Token);
        }

        public virtual async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
        {
            if (!_accessToken.HasValue || AccessTokenExpired)
            {
                _accessToken = await _tokenCredential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken).ConfigureAwait(false);
            }

            return _accessToken.Value;
        }

        protected virtual bool AccessTokenExpired
        {
            get { return !_accessToken.HasValue || (DateTime.UtcNow + ExpirationThreshold >= _accessToken.Value.ExpiresOn); }
        }
    }
}

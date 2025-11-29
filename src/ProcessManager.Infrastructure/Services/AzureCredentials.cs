using Azure.Core;
using Azure.Identity;
using System;

namespace ProcessManager.Infrastructure.Services
{
    public static class AzureCredentials
    {
        public static TokenCredential GetDefaultCredentials()
        {
            if (IsDevelopment)
            {
                return new DefaultAzureCredential();
            }

            return new ManagedIdentityCredential();
        }

        public static TokenCredential GetCliCredentials()
        {
            if (IsDevelopment)
            {
                return new AzureCliCredential();
            }

            return new ManagedIdentityCredential();
        }

        private static bool IsDevelopment => "Development".Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            StringComparison.InvariantCultureIgnoreCase
        );
    }
}

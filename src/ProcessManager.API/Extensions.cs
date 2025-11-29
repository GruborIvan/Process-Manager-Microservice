using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;

namespace ProcessManager.API.HealthChecks
{
    public static class Extensions
    {
        public static IHealthChecksBuilder AddServiceHealthCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default) where T : class, IHealthCheck
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                sp => sp.GetRequiredService(typeof(T)) as T,
                failureStatus,
                tags));
        }
    }
}

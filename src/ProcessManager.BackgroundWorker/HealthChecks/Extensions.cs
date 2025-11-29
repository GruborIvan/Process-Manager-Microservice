using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;

namespace ProcessManager.BackgroundWorker.HealthChecks
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

        public static IHealthChecksBuilder AddServiceHealthCheck(
            this IHealthChecksBuilder builder,
            string name,
            IHealthCheck healthChecker,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default
        )
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                healthChecker,
                failureStatus,
                tags));
        }

        public static IHealthChecksBuilder AddServiceHealthCheck<T>(
            this IHealthChecksBuilder builder,
            string name,
            HealthStatus? failureStatus = default,
            IEnumerable<string>? tags = default,
            params object[] args) where T : class, IHealthCheck
        {
            return builder.AddTypeActivatedCheck<T>(
                name,
                failureStatus,
                tags,
                args);
        }
    }
}

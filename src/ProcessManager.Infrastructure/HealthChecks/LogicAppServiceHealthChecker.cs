using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProcessManager.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessManager.Infrastructure.HealthChecks
{
    public class LogicAppServiceHealthChecker : IHealthCheck
    {
        private readonly IProcessService _processService;
        public LogicAppServiceHealthChecker(IProcessService service)
        {
            _processService = service;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _processService.GetProcessWithMessageAsync("Task", "Task", new { }, null);
                return HealthCheckResult.Healthy("Process definitions are accessible");
            }
            catch(Exception e)
            {
                return HealthCheckResult.Unhealthy("Check resulted in an exception", e);
            }
        }
    }
}

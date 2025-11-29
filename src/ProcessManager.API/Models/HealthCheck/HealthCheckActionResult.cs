using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;

namespace ProcessManager.API.Models.HealthCheck
{
    public class HealthCheckActionResult : ActionResult
    {
        private readonly HealthCheckOptions _healthCheckOptions;
        private readonly HealthCheckService _healthCheckService;

        public HealthCheckActionResult(HealthCheckService healthCheckService, HealthCheckOptions healthCheckOptions)
        {
            _healthCheckOptions = healthCheckOptions;
            _healthCheckService = healthCheckService;
        }

        public HealthReport HealthReport { get; private set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            HealthReport = await _healthCheckService.CheckHealthAsync(_healthCheckOptions.Predicate, context.HttpContext.RequestAborted);
            context.HttpContext.Response.StatusCode = _healthCheckOptions.ResultStatusCodes[HealthReport.Status];
            await _healthCheckOptions.ResponseWriter(context.HttpContext, HealthReport);
        }
    }
}

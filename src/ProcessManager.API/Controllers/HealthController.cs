using Matrix.HealthCheckers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProcessManager.API.Models.HealthCheck;

namespace ProcessManager.API.Controllers
{
    [Route("health")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            this.healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Get Liveness Health Check Status
        /// </summary>
        /// <returns>Liveness health check status</returns>
        [HttpGet("liveness")]
        public ActionResult Liveness()
        {
            var healthCheckOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("liveness"),
                ResponseWriter = HealthCheckWriters.WriteHealthCheckLiveResponse,
                AllowCachingResponses = false
            };

            return new HealthCheckActionResult(healthCheckService, healthCheckOptions);
        }

        /// <summary>
        /// Get Readiness Health Check Status
        /// </summary>
        /// <returns>Readiness health check status</returns>
        [HttpGet("readiness")]
        public ActionResult Readiness()
        {
            var healthCheckOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
            {
                Predicate = (check) => check.Tags.Contains("readiness"),
                ResponseWriter = HealthCheckWriters.WriteHealthCheckReadyResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            };

            return new HealthCheckActionResult(healthCheckService, healthCheckOptions);
        }
    }
}

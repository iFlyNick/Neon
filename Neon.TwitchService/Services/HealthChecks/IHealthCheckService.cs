using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Neon.TwitchService.Services.HealthChecks;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken ct = default);
}
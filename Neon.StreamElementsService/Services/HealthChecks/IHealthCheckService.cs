using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Neon.StreamElementsService.Services.HealthChecks;

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken ct = default);
}
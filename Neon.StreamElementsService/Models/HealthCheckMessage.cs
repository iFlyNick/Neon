using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Neon.StreamElementsService.Models;

public class HealthCheckMessage
{
    public string? ServiceName { get; set; }
    public string? Description { get; set; }
    public HealthStatus? OverallStatus { get; set; }
    public DateTime? Timestamp { get; set; }
}
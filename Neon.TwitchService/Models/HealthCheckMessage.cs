namespace Neon.TwitchService.Models;

public class HealthCheckMessage
{
    public string? ServiceName { get; set; }
    public string? Description { get; set; }
    public DateTime? Timestamp { get; set; }
}
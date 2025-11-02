namespace Neon.TwitchService.Models;

public class WebSocketSubscriptionDetail
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? CreatedAt { get; set; }
    public string? ConnectedAt { get; set; }
    public string? DisconnectedAt { get; set; }
}
using Neon.Core.Models.Twitch.Helix.WebSockets;

namespace Neon.TwitchService.Models;

public class WebSocketHealthDetail
{
    public string? SessionId { get; set; }
    public string? Channel { get; set; }
    public string? ChatUser { get; set; }
    public bool? IsConnected { get; set; }
    public List<WebSocketSubscription>? Subscriptions { get; set; }
}
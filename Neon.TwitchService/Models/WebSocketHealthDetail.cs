using Neon.Core.Models.Twitch.Helix.WebSockets;

namespace Neon.TwitchService.Models;

public class WebSocketHealthDetail
{
    public string? SessionId { get; set; }
    public string? BroadcasterId { get; set; }
    public string? ChatterId { get; set; }
    public bool? IsConnected { get; set; }
    public List<WebSocketSubscriptionDetail>? Subscriptions { get; set; }
}
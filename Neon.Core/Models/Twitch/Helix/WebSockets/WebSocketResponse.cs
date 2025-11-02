using System.Text.Json.Serialization;

namespace Neon.Core.Models.Twitch.Helix.WebSockets;

public class WebSocketResponse
{
    [JsonPropertyName("data")]
    public List<WebSocketSubscription>? Subscriptions { get; set; }
}
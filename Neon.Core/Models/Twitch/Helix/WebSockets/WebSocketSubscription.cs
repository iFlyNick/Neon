using Neon.Core.Models.Twitch.EventSub;
using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.Helix.WebSockets;

public class WebSocketSubscription
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("status")]
    public string? Status { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("version")]
    public string? Version { get; set; }
    [JsonProperty("condition")]
    public Condition? Condition { get; set; }
    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }
    [JsonProperty("transport")]
    public Transport? Transport { get; set; }
    [JsonProperty("disconnected_at")]
    public string? DisconnectedAt { get; set; }
    [JsonProperty("cost")]
    public int? Cost { get; set; }
}
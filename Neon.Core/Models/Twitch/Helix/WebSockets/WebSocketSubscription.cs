using System.Text.Json.Serialization;
using Neon.Core.Models.Twitch.EventSub;

namespace Neon.Core.Models.Twitch.Helix.WebSockets;

public class WebSocketSubscription
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("version")]
    public string? Version { get; set; }
    [JsonPropertyName("condition")]
    public Condition? Condition { get; set; }
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
    [JsonPropertyName("transport")]
    public Transport? Transport { get; set; }
    [JsonPropertyName("disconnected_at")]
    public string? DisconnectedAt { get; set; }
    [JsonPropertyName("cost")]
    public int? Cost { get; set; }
    [JsonPropertyName("total")]
    public int? Total { get; set; }
    [JsonPropertyName("total_cost")]
    public int? TotalCost { get; set; }
    [JsonPropertyName("max_total_cost")]
    public int? MaxTotalCost { get; set; }
    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; set; }
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}
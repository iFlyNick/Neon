using Neon.Core.Models.Twitch.EventSub;
using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.Helix.WebSockets;

public class WebSocketResponse
{
    [JsonProperty("total")]
    public int? Total { get; set; }
    [JsonProperty("data")]
    public List<WebSocketSubscription>? Subscriptions { get; set; }
    [JsonProperty("max_total_cost")]
    public int? MaxTotalCost { get; set; }
    [JsonProperty("total_cost")]
    public int? TotalCost { get; set; }
    [JsonProperty("pagination")]
    public Pagination? Pagination { get; set; }
}
using Neon.Core.Models.Twitch.EventSub;
using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Subscription
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("status")]
    public string? Status { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("version")]
    public string? Version { get; set; }
    [JsonProperty("cost")]
    public int? Cost { get; set; }
    [JsonProperty("condition")]
    public Condition? Condition { get; set; }
    [JsonProperty("transport")]
    public Transport? Transport { get; set; }
    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }
}

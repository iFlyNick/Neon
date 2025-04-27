using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Reward
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("title")]
    public string? Title { get; set; }
    [JsonProperty("cost")]
    public int? Cost { get; set; }
    [JsonProperty("prompt")]
    public string? Prompt { get; set; }
}

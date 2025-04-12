using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

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

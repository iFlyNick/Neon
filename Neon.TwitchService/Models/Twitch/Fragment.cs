using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class Fragment
{
    [JsonProperty("text")]
    public string? Text { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("emote")]
    public Emote? Emote { get; set; }
    [JsonProperty("cheermote")]
    public Cheermote? Cheermote { get; set; }
}

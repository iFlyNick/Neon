using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class TwitchMessage
{
    [JsonProperty("text")]
    public string? Text { get; set; }
    [JsonProperty("fragments")]
    public List<Fragment>? Fragments { get; set; }
}

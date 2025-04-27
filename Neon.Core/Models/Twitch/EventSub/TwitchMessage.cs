using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class TwitchMessage
{
    [JsonProperty("text")]
    public string? Text { get; set; }
    [JsonProperty("fragments")]
    public List<Fragment>? Fragments { get; set; }
}

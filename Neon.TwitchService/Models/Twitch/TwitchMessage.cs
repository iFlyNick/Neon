using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class TwitchMessage
{
    //[JsonProperty("fragments")]
    //public List<MessageFragment>? Fragments { get; set; }
    [JsonProperty("text")]
    public string? Text { get; set; }
}

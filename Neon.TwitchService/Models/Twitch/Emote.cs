using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class Emote
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("name")]
    public string? Name { get; set; }
    [JsonProperty("emote_set_id")]
    public string? EmoteSetId { get; set; }
    [JsonProperty("owner_id")]
    public string? OwnerId { get; set; }
    [JsonProperty("format")]
    public List<string>? Format { get; set; }
}

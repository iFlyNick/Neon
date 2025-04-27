using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Badge
{
    [JsonProperty("set_id")]
    public string? SetId { get; set; }
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("info")]
    public string? Info { get; set; }
}
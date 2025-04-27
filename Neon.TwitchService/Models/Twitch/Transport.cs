using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class Transport
{
    [JsonProperty("method")]
    public string? Method { get; set; }
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }
}

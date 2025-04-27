using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Transport
{
    [JsonProperty("method")]
    public string? Method { get; set; }
    [JsonProperty("session_id")]
    public string? SessionId { get; set; }
}

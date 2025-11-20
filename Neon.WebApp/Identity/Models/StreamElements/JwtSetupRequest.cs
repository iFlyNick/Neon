using Newtonsoft.Json;

namespace Neon.WebApp.Identity.Models.StreamElements;

public class JwtSetupRequest
{
    [JsonProperty("broadcasterId")]
    public string? TwitchBroadcasterId { get; set; }
    [JsonProperty("channelId")]
    public string? StreamElementsChannelId { get; set; }
    [JsonProperty("jwtToken")]
    public string? JwtToken { get; set; }
}
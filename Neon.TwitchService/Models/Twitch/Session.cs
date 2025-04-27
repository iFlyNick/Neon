using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class Session
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    [JsonProperty("keepalive_timeout_seconds")]
    public int KeepAliveTimeoutSeconds { get; set; }
    [JsonProperty("reconnect_url")]
    public string? ReconnectUrl { get; set; }
    [JsonProperty("connected_at")]
    public string? ConnectedAt { get; set; }
}

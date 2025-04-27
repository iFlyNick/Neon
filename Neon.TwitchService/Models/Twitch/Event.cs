using Newtonsoft.Json;

namespace Neon.TwitchService.Models.Twitch;

public class Event
{
    //public List<Badge>? Badges { get; set; }
    [JsonProperty("broadcaster_user_id")]
    public string? BroadcasterUserId { get; set; }
    [JsonProperty("broadcaster_user_login")]
    public string? BroadcasterUserLogin { get; set; }
    [JsonProperty("broadcaster_user_name")]
    public string? BroadcasterUserName { get; set; }
    [JsonProperty("chatter_user_id")]
    public string? ChatterUserId { get; set; }
    [JsonProperty("chatter_user_login")]
    public string? ChatterUserLogin { get; set; }
    [JsonProperty("chatter_user_name")]
    public string? ChatterUserName { get; set; }
    [JsonProperty("message")]
    public TwitchMessage? TwitchMessage { get; set; }
    [JsonProperty("message_type")]
    public string? MessageType { get; set; }
}

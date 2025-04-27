using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Condition
{
    [JsonProperty("user_id")]
    public string? UserId { get; set; }
    [JsonProperty("broadcaster_user_id")]
    public string? BroadcasterUserId { get; set; }
    [JsonProperty("moderator_user_id")]
    public string? ModeratorUserId { get; set; }
}

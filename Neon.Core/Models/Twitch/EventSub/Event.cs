using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class Event
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("broadcaster_user_id")]
    public string? BroadcasterUserId { get; set; }
    [JsonProperty("broadcaster_user_login")]
    public string? BroadcasterUserLogin { get; set; }
    [JsonProperty("broadcaster_user_name")]
    public string? BroadcasterUserName { get; set; }
    [JsonProperty("moderator_user_id")]
    public string? ModeratorUserId { get; set; }
    [JsonProperty("moderator_user_login")]
    public string? ModeratorUserLogin { get; set; }
    [JsonProperty("moderator_user_name")]
    public string? ModeratorUserName { get; set; }
    [JsonProperty("requester_user_id")]
    public string? RequesterUserId { get; set; }
    [JsonProperty("requester_user_login")]
    public string? RequesterUserLogin { get; set; }
    [JsonProperty("requester_user_name")]
    public string? RequesterUserName { get; set; }
    [JsonProperty("chatter_user_id")]
    public string? ChatterUserId { get; set; }
    [JsonProperty("chatter_user_login")]
    public string? ChatterUserLogin { get; set; }
    [JsonProperty("chatter_user_name")]
    public string? ChatterUserName { get; set; }
    [JsonProperty("user_id")]
    public string? UserId { get; set; }
    [JsonProperty("user_login")]
    public string? UserLogin { get; set; }
    [JsonProperty("user_name")]
    public string? UserName { get; set; }
    [JsonProperty("user_input")]
    public string? UserInput { get; set; }
    [JsonProperty("status")]
    public string? Status { get; set; }
    [JsonProperty("reward")]
    public Reward? Reward { get; set; }
    [JsonProperty("redeemed_at")]
    public string? RedeemedAt { get; set; }
    [JsonProperty("bits")]
    public int? Bits { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    public string? Reason { get; set; }
    [JsonProperty("banned_at")]
    public string? BannedAt { get; set; }
    [JsonProperty("ends_at")]
    public string? EndsAt { get; set; }
    [JsonProperty("is_permanent")]
    public bool? IsPermanent { get; set; }
    [JsonProperty("duration_seconds")]
    public int? DurationSeconds { get; set; }
    [JsonProperty("started_at")]
    public string? StartedAt { get; set; }
    [JsonProperty("is_automatic")]
    public bool? IsAutomatic { get; set; }
    [JsonProperty("message")]
    public TwitchMessage? TwitchMessage { get; set; }
    [JsonProperty("message_type")]
    public string? MessageType { get; set; }
    [JsonProperty("badges")]
    public List<Badge>? Badges { get; set; }
    [JsonProperty("color")]
    public string? Color { get; set; }
    [JsonProperty("title")]
    public string? Title { get; set; }
    [JsonProperty("category_id")]
    public string? CategoryId { get; set; }
    [JsonProperty("category_name")]
    public string? CategoryName { get; set; }
    [JsonProperty("content_classification_labels")]
    public List<string>? ContentClassificationLabels { get; set; }
    [JsonProperty("power_up")]
    public PowerUp? PowerUp { get; set; }
}

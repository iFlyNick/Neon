namespace Neon.Core.Models.Twitch;

public class TwitchUserAccount
{
    public string? BroadcasterId { get; set; }
    public string? LoginName { get; set; }
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
    public string? BroadcasterType { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? OfflineImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
}

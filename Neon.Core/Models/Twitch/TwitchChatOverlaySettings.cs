namespace Neon.Core.Models.Twitch;

public class TwitchChatOverlaySettings
{
    public string? BroadcasterId { get; set; }
    public string? OverlayName { get; set; }
    public string? OverlayUrl { get; set; }
    public string? ChatStyle { get; set; }
    public bool? IgnoreBotMessages { get; set; }
    public bool? IgnoreCommandMessages { get; set; }
    public bool? UseTwitchBadges { get; set; }
    public bool? UseBetterTtvEmotes { get; set; }
    public bool? UseSevenTvEmotes { get; set; }
    public bool? UseFfzEmotes { get; set; }
    public int? ChatDelayMilliseconds { get; set; }
    public bool? AlwaysKeepMessages { get; set; }
    public int? ChatMessageRemoveDelayMilliseconds { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
}
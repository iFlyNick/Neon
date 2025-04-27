namespace Neon.Obs.BrowserSource.WebApp.Models;

public class TwitchMessage
{
    public string? ChannelName { get; set; }
    public string? ChatterName { get; set; }
    public string? ChatterColor { get; set; }
    public List<ProviderBadge>? ChatterBadges { get; set; }
    public string? Message { get; set; }
}
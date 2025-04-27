using Neon.TwitchMessageService.Models.Badges;

namespace Neon.TwitchMessageService.Models;

public class ProcessedMessage
{
    public string? ChannelName { get; set; }
    public string? ChatterName { get; set; }
    public string? ChatterColor { get; set; }
    public List<ProviderBadge>? ChatterBadges { get; set; }
    public string? Message { get; set; }
}

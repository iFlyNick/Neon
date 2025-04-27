namespace Neon.TwitchMessageService.Models.Badges;

public class ProviderBadgeSet
{
    public string? SetId { get; set; }
    public List<ProviderBadge>? ProviderBadges { get; set; }
}
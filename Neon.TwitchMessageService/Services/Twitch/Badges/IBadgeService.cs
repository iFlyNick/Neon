using Neon.Core.Models.Twitch.EventSub;
using Neon.TwitchMessageService.Models.Badges;

namespace Neon.TwitchMessageService.Services.Twitch.Badges;

public interface IBadgeService
{
    Task PreloadGlobalBadgesAsync(CancellationToken ct = default);
    Task PreloadChannelBadgesAsync(string? broadcasterId, CancellationToken ct = default);
    Task<List<ProviderBadge>?> GetProviderBadgesFromBadges(string? broadcasterId, List<Badge>? badges, CancellationToken ct = default);
}
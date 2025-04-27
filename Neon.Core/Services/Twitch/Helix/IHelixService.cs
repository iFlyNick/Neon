using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Helix;

public interface IHelixService
{
    Task<string?> GetGlobalEmotes(CancellationToken ct = default);
    Task<string?> GetChannelEmotes(string? broadcasterId, CancellationToken ct = default);
    Task<string?> GetGlobalBadges(CancellationToken ct = default);
    Task<string?> GetChannelBadges(string? broadcasterId, CancellationToken ct = default);
    Task<TwitchUserAccount?> GetUserAccountDetailsAsync(string? broadcasterId, string? appAccessToken, CancellationToken ct = default);
    Task SendMessageAsUser(string? message, string? userId, string? broadcasterId, CancellationToken ct = default);
}

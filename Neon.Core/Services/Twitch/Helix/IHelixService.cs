using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Helix;

public interface IHelixService
{
    Task<string?> GetGlobalEmotes(CancellationToken ct = default);
    Task<string?> GetChannelEmotes(string? broadcasterId, CancellationToken ct = default);
    Task<TwitchUserAccount?> GetUserAccountDetailsAsync(string? broadcasterId, string? appAccessToken, CancellationToken ct = default);
    Task SendMessageAsBot(string? message, string? chatBotId, string? broadcasterId, CancellationToken ct = default);
}

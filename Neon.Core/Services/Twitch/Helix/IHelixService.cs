using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Helix;

public interface IHelixService
{
    Task<TwitchUserAccount?> GetUserAccountDetailsAsync(string? broadcasterId, string? appAccessToken, CancellationToken ct = default);
}

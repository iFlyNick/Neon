using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public interface IBotTokenService
{
    Task<string?> GetBotClientIdAsync(CancellationToken ct = default);
    Task<OAuthResponse?> GetBotAccountAuthAsync(CancellationToken ct = default);
}

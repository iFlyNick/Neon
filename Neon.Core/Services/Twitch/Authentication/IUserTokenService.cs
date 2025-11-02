using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public interface IUserTokenService
{
    Task<OAuthResponse?> GetUserAccountAuthAsync(string? userCode, string? botAccessToken, CancellationToken ct = default);
    Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default);
    Task EnsureUserTokenValidByBroadcasterId(string? broadcasterId, CancellationToken ct = default);
    Task EnsureUserTokenValidByBroadcasterName(string? broadcasterName, CancellationToken ct = default);
    Task<string?> GetUserAuthTokenByBroadcasterId(string? broadcasterId, CancellationToken ct = default);
    Task<string?> GetUserAuthTokenByBroadcasterName(string? broadcasterName, CancellationToken ct = default);
}

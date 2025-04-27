using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public interface IOAuthService
{
    Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default);
    Task<OAuthResponse?> GetAppAuthToken(string? clientId, string? clientSecret, CancellationToken ct = default);
    Task<OAuthResponse?> GetUserAuthToken(string? clientId, string? clientSecret, string? userCode, string? redirectUri);
    Task<OAuthResponse?> GetUserAuthTokenFromRefresh(string? clientId, string? clientSecret, string? refeshToken);
}

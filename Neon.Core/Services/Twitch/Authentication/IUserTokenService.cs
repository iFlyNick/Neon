using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public interface IUserTokenService
{
    Task<OAuthResponse?> GetUserAccountAuthAsync(string? userCode, string? botAccessToken, CancellationToken ct = default);
    Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default);
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class UserTokenService(ILogger<UserTokenService> logger, IOAuthService oAuthService, ITwitchDbService twitchDbService, IOptions<NeonSettings> twitchAppSettings) : IUserTokenService
{
    private readonly ILogger<UserTokenService> _logger = logger;
    private readonly IOAuthService _oAuthService = oAuthService;
    private readonly ITwitchDbService _twitchDbService = twitchDbService;
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value;

    public async Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(authToken))
        {
            _logger.LogWarning("Unable to validate auth token as token is null or empty.");
            return null;
        }

        var validationResponse = await _oAuthService.ValidateOAuthToken(authToken, ct);

        return validationResponse;
    }

    public async Task<OAuthResponse?> GetUserAccountAuthAsync(string? userCode, string? appAccessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userCode))
        {
            _logger.LogInformation("Missing user code for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(appAccessToken))
        {
            _logger.LogInformation("Missing app access token for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            _logger.LogInformation("Missing app name for Auth.");
            return null;
        }

        var appAccount = await _twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            _logger.LogInformation("App account not found. AppName: {appName}", _twitchAppSettings.AppName);
            return null;
        }

        var userAuth = await _oAuthService.GetUserAuthToken(appAccount.ClientId, appAccount.ClientSecret, userCode, appAccount.RedirectUri, ct);

        return userAuth;
    }
}

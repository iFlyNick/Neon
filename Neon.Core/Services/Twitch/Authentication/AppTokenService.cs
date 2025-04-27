using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class AppTokenService(ILogger<AppTokenService> logger, ITwitchDbService twitchDbService, IOAuthService oAuthService, IOptions<NeonSettings> twitchAppSettings) : IAppTokenService
{
    private readonly ILogger<AppTokenService> _logger = logger;
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value;
    private readonly IOAuthService _oAuthService = oAuthService;
    private readonly ITwitchDbService _twitchDbService = twitchDbService;

    public async Task<string?> GetAppClientIdAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            _logger.LogCritical("App name is not set in the configuration.");
            return null;
        }

        var appAccount = await _twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            _logger.LogCritical("App account not found in the database. AppName: {appName}", _twitchAppSettings.AppName);
            return null;
        }

        return appAccount.ClientId;
    }

    public async Task<OAuthResponse?> GetAppAccountAuthAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            _logger.LogCritical("App name is not set in the configuration.");
            return null;
        }

        var appAccount = await _twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            _logger.LogCritical("App account not found in the database. AppName: {AppName}", _twitchAppSettings.AppName);
            return null;
        }

        if (string.IsNullOrEmpty(appAccount.ClientId) || string.IsNullOrEmpty(appAccount.ClientSecret))
        {
            _logger.LogCritical("App account is missing client id or client secret. AppName: {AppName}", _twitchAppSettings.AppName);
            return null;
        }

        if (string.IsNullOrEmpty(appAccount.AccessToken))
        {
            _logger.LogDebug("App account is missing access token from db. Attempting to fetch new token.");
            var missingTokenResp = await _oAuthService.GetAppAuthToken(appAccount.ClientId, appAccount.ClientSecret, ct);

            return missingTokenResp;
        }

        //check if current db token is valid as one at least exists
        try
        {
            //TODO: this would check every time it's called to see if it's valid. could do that, or add some internal padding to check every x interval instead
            var oAuthValidation = await _oAuthService.ValidateOAuthToken(appAccount.AccessToken, ct);

            ArgumentNullException.ThrowIfNull(oAuthValidation, "OAuth validation response is null");

            //if this gets here, build a fake oauth response to mimic what twitch would've returned and send it back early
            var earlyResp = new OAuthResponse
            {
                AccessToken = appAccount.AccessToken,
                TokenType = "Bearer",
                Scope = oAuthValidation.Scopes
            };

            return earlyResp;
        }
        catch (Exception)
        {
            _logger.LogDebug("App account access token indicates invalid. Will attempt to fetch a new one");
        }

        _logger.LogDebug("Fetching new app access token from twitch.");
        var resp = await _oAuthService.GetAppAuthToken(appAccount.ClientId, appAccount.ClientSecret, ct);

        if (resp is null || string.IsNullOrEmpty(resp.AccessToken))
        {
            _logger.LogCritical("Failed to fetch new app access token from twitch.");
            return null;
        }

        //store updated details for next access attempt
        appAccount.AccessToken = resp.AccessToken;
        await _twitchDbService.UpdateAppAccountSettingsAsync(appAccount, ct);

        return resp;
    }
}

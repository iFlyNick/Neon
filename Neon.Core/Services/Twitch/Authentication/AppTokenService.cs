using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class AppTokenService(ILogger<AppTokenService> logger, ITwitchDbService twitchDbService, IOAuthService oAuthService, IOptions<NeonSettings> twitchAppSettings) : IAppTokenService
{
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value;

    public async Task<string?> GetAppClientIdAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            logger.LogCritical("App name is not set in the configuration.");
            return null;
        }

        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            logger.LogCritical("App account not found in the database. AppName: {appName}", _twitchAppSettings.AppName);
            return null;
        }

        return appAccount.ClientId;
    }
    
    public async Task<string?> GetAppClientSecretAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            logger.LogCritical("App name is not set in the configuration.");
            return null;
        }

        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            logger.LogCritical("App account not found in the database. AppName: {appName}", _twitchAppSettings.AppName);
            return null;
        }

        return appAccount.ClientSecret;
    }

    public async Task<OAuthResponse?> GetAppAccountAuthAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            logger.LogCritical("App name is not set in the configuration.");
            return null;
        }

        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            logger.LogCritical("App account not found in the database. AppName: {AppName}", _twitchAppSettings.AppName);
            return null;
        }

        if (string.IsNullOrEmpty(appAccount.ClientId) || string.IsNullOrEmpty(appAccount.ClientSecret))
        {
            logger.LogCritical("App account is missing client id or client secret. AppName: {AppName}", _twitchAppSettings.AppName);
            return null;
        }

        if (string.IsNullOrEmpty(appAccount.AccessToken))
        {
            logger.LogDebug("App account is missing access token from db. Attempting to fetch new token.");
            var missingTokenResp = await oAuthService.GetAppAuthToken(appAccount.ClientId, appAccount.ClientSecret, ct);

            return missingTokenResp;
        }

        //check if current db token is valid as one at least exists
        try
        {
            //TODO: this would check every time it's called to see if it's valid. could do that, or add some internal padding to check every x interval instead
            var oAuthValidation = await oAuthService.ValidateOAuthToken(appAccount.AccessToken, ct);

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
            logger.LogDebug("App account access token indicates invalid. Will attempt to fetch a new one");
        }

        logger.LogDebug("Fetching new app access token from twitch.");
        var resp = await oAuthService.GetAppAuthToken(appAccount.ClientId, appAccount.ClientSecret, ct);

        if (resp is null || string.IsNullOrEmpty(resp.AccessToken))
        {
            logger.LogCritical("Failed to fetch new app access token from twitch.");
            return null;
        }

        //store updated details for next access attempt
        appAccount.AccessToken = resp.AccessToken;
        await twitchDbService.UpdateAppAccountSettingsAsync(appAccount, ct);

        return resp;
    }

    public async Task EnsureAppTokenValid(CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;
using Neon.Persistence.EntityModels.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class UserTokenService(ILogger<UserTokenService> logger, IOAuthService oAuthService, ITwitchDbService twitchDbService, IOptions<NeonSettings> twitchAppSettings, IAppTokenService appTokenService) : IUserTokenService
{
    private readonly NeonSettings _twitchAppSettings = twitchAppSettings.Value;

    public async Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(authToken))
        {
            logger.LogWarning("Unable to validate auth token as token is null or empty.");
            return null;
        }

        var validationResponse = await oAuthService.ValidateOAuthToken(authToken, ct);

        return validationResponse;
    }

    public async Task<OAuthResponse?> GetUserAccountAuthAsync(string? userCode, string? appAccessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userCode))
        {
            logger.LogInformation("Missing user code for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(appAccessToken))
        {
            logger.LogInformation("Missing app access token for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(_twitchAppSettings.AppName))
        {
            logger.LogInformation("Missing app name for Auth.");
            return null;
        }

        var appAccount = await twitchDbService.GetAppAccountAsync(_twitchAppSettings.AppName, ct);

        if (appAccount is null)
        {
            logger.LogInformation("App account not found. AppName: {appName}", _twitchAppSettings.AppName);
            return null;
        }

        var userAuth = await oAuthService.GetUserAuthToken(appAccount.ClientId, appAccount.ClientSecret, userCode, appAccount.RedirectUri, ct);

        return userAuth;
    }

    public async Task<string?> GetUserAuthTokenByBroadcasterId(string? broadcasterId, CancellationToken ct = default)
    {
        await EnsureUserTokenValidByBroadcasterId(broadcasterId, ct);
        var twitchAccount = await twitchDbService.GetTwitchAccountByBroadcasterIdAsync(broadcasterId, ct);
        return twitchAccount?.TwitchAccountAuth?.AccessToken;
    }
    
    public async Task<string?> GetUserAuthTokenByBroadcasterName(string? broadcasterName, CancellationToken ct = default)
    {
        await EnsureUserTokenValidByBroadcasterName(broadcasterName, ct);
        var twitchAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        return twitchAccount?.TwitchAccountAuth?.AccessToken;
    }
    
    public async Task EnsureUserTokenValidByBroadcasterId(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogDebug("Cannot ensure user token validity. Broadcaster ID is null or empty.");
            throw new ArgumentNullException(nameof(broadcasterId));
        }
        
        var twitchAccount = await twitchDbService.GetTwitchAccountByBroadcasterIdAsync(broadcasterId, ct);
        await EnsureUserTokenValid(twitchAccount, twitchAccount?.LoginName, ct);
    }
    
    public async Task EnsureUserTokenValidByBroadcasterName(string? broadcasterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterName))
        {
            logger.LogDebug("Cannot ensure user token validity. Broadcaster Name is null or empty.");
            throw new ArgumentNullException(nameof(broadcasterName));
        }
        
        var twitchAccount = await twitchDbService.GetTwitchAccountByBroadcasterName(broadcasterName, ct);
        await EnsureUserTokenValid(twitchAccount, broadcasterName, ct);
    }

    private async Task EnsureUserTokenValid(TwitchAccount? account, string? broadcasterName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(broadcasterName);
        ArgumentNullException.ThrowIfNull(account.TwitchAccountAuth?.AccessToken);
        
        var oAuthValidation = await ValidateOAuthToken(account.TwitchAccountAuth.AccessToken, ct);
        
        if (oAuthValidation is null)
        {
            var appClientId = await appTokenService.GetAppClientIdAsync(ct);
            var appClientSecret = await appTokenService.GetAppClientSecretAsync(ct);
            
            logger.LogDebug("Access token needs refreshed or is invalid for account: {broadcasterName}", broadcasterName);
            var oAuthResp = await oAuthService.GetUserAuthTokenFromRefresh(appClientId, appClientSecret, account.TwitchAccountAuth.RefreshToken, ct);

            if (oAuthResp is null || string.IsNullOrEmpty(oAuthResp.AccessToken))
            {
                logger.LogError("Failed to refresh access token for account: {broadcasterName}", broadcasterName);
                return;
            }

            account.TwitchAccountAuth.AccessToken = oAuthResp.AccessToken;

            //update db with new access token
            _ = await twitchDbService.UpsertTwitchAccountAsync(account, ct);
            
            logger.LogDebug("Successfully refreshed access token for account: {broadcasterName}", broadcasterName);
        }
    }
}

using Neon.Core.Services.Twitch.Authentication;
using Neon.Core.Services.Twitch.Helix;
using Neon.WebApp.Identity.Models.Twitch;

namespace Neon.WebApp.Identity.Twitch;

public class TwitchAuthResponseService(ILogger<TwitchAuthResponseService> logger, IAppTokenService appTokenService, IUserTokenService userTokenService, ITwitchAccountService twitchAccountService, IHelixService helixService) : ITwitchAuthResponseService
{
    public async Task HandleResponseAsync(AuthenticationResponse? response, CancellationToken ct = default)
    {
        if (response is null)
        {
            logger.LogError("Twitch Authentication response received with no data!");
            return;
        }
        
        if (response.Error is not null)
            HandleAuthErrorRequest(response);

        await HandleAuthSuccessRequest(response, ct);
    }

    private async Task HandleAuthSuccessRequest(AuthenticationResponse response, CancellationToken ct = default)
    {
        //at this point we have a code, but don't have any details about who it was. take the token and call the validate method against the oauth service to get a bit larger picture next
        var appAuth = await appTokenService.GetAppAccountAuthAsync(ct);

        if (appAuth is null || string.IsNullOrEmpty(appAuth.AccessToken))
        {
            logger.LogCritical("Failed to fetch app account from database or failed to get access token!");
            return;
        }

        var userAuth = await userTokenService.GetUserAccountAuthAsync(response.Code, appAuth.AccessToken, ct);

        if (userAuth is null || string.IsNullOrEmpty(userAuth.AccessToken))
        {
            logger.LogError("Failed to get user auth token from twitch!");
            return;
        }

        var userAuthValidation = await userTokenService.ValidateOAuthToken(userAuth.AccessToken, ct);

        if (userAuthValidation is null)
        {
            logger.LogError("User auth token validation failed! Unable to create local twitch account representation!");
            return;
        }

        var twitchUserDetails = await helixService.GetUserAccountDetailsAsync(userAuthValidation.UserId, userAuth.AccessToken, ct);

        var twitchUserAuth = new TwitchUserAccountAuth
        {
            AuthenticationResponse = response,
            OAuthResponse = userAuth,
            OAuthValidationResponse = userAuthValidation,
            TwitchUserAccount = twitchUserDetails
        };

        await twitchAccountService.CreateTwitchAccountFromOAuthAsync(twitchUserAuth, ct);
    }

    private void HandleAuthErrorRequest(AuthenticationResponse response)
    {
        //log for now, could take action later
        logger.LogError("Error: {Error}, Description: {ErrorDescription}", response.Error, response.ErrorDescription);
        return;
    }
}
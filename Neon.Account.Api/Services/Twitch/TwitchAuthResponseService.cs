using Neon.Account.Api.Models;
using Neon.Account.Api.Models.Twitch;
using Neon.Core.Services.Twitch.Authentication;
using Neon.Core.Services.Twitch.Helix;

namespace Neon.Account.Api.Services.Twitch;

public class TwitchAuthResponseService(ILogger<TwitchAuthResponseService> logger, IBotTokenService botTokenService, IUserTokenService userTokenService, ITwitchAccountService twitchAccountService, IHelixService helixService) : ITwitchAuthResponseService
{
    private readonly ILogger<TwitchAuthResponseService> _logger = logger;
    private readonly IBotTokenService _botTokenService = botTokenService;
    private readonly IUserTokenService _userTokenService = userTokenService;
    private readonly ITwitchAccountService _twitchAccountService = twitchAccountService;
    private readonly IHelixService _helixService = helixService;

    public async Task HandleResponseAsync(AuthenticationResponse? response, CancellationToken ct = default)
    {
        if (response is null)
        {
            _logger.LogError("Twitch Authentication response received with no data!");
            return;
        }
        
        if (response.Error is not null)
            HandleAuthErrorRequest(response);

        await HandleAuthSuccessRequest(response, ct);
    }

    private async Task HandleAuthSuccessRequest(AuthenticationResponse response, CancellationToken ct = default)
    {
        //at this point we have a code, but dont have any details about who it was. take the token and call the validate method against the oauth service to get a bit larger picture next
        var botAuth = await _botTokenService.GetBotAccountAuthAsync(ct);

        if (botAuth is null || string.IsNullOrEmpty(botAuth.AccessToken))
        {
            _logger.LogCritical("Failed to fetch bot account from database or failed to get access token!");
            return;
        }

        var userAuth = await _userTokenService.GetUserAccountAuthAsync(response.Code, botAuth.AccessToken, ct);

        if (userAuth is null || string.IsNullOrEmpty(userAuth.AccessToken))
        {
            _logger.LogError("Failed to get user auth token from twitch!");
            return;
        }

        var userAuthValidation = await _userTokenService.ValidateOAuthToken(userAuth.AccessToken, ct);

        if (userAuthValidation is null)
        {
            _logger.LogError("User auth token validation failed! Unable to create local twitch account representation!");
            return;
        }

        var twitchUserDetails = await _helixService.GetUserAccountDetailsAsync(userAuthValidation.UserId, userAuth.AccessToken, ct);

        var twitchUserAuth = new TwitchUserAccountAuth
        {
            AuthenticationResponse = response,
            OAuthResponse = userAuth,
            OAuthValidationResponse = userAuthValidation,
            TwitchUserAccount = twitchUserDetails
        };

        await _twitchAccountService.CreateTwitchAccountFromOAuthAsync(twitchUserAuth, ct);
    }

    private void HandleAuthErrorRequest(AuthenticationResponse response)
    {
        //log for now, could take action later
        _logger.LogError("Error: {Error}, Description: {ErrorDescription}", response.Error, response.ErrorDescription);
        return;
    }
}

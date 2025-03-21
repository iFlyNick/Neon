using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class BotTokenService(ILogger<BotTokenService> logger, ITwitchDbService twitchDbService, IOAuthService oAuthService, IOptions<NeonSettings> botSettings) : IBotTokenService
{
    private readonly ILogger<BotTokenService> _logger = logger;
    private readonly NeonSettings _botSettings = botSettings.Value;
    private readonly IOAuthService _oAuthService = oAuthService;
    private readonly ITwitchDbService _twitchDbService = twitchDbService;

    public async Task<string?> GetBotClientIdAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_botSettings.BotName))
        {
            _logger.LogCritical("Bot name is not set in the configuration.");
            return null;
        }

        var botAccount = await _twitchDbService.GetBotAccountAsync(_botSettings.BotName, ct);

        if (botAccount is null)
        {
            _logger.LogCritical("Bot account not found in the database. BotName: {botName}", _botSettings.BotName);
            return null;
        }

        return botAccount.ClientId;
    }

    public async Task<OAuthResponse?> GetBotAccountAuthAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_botSettings.BotName))
        {
            _logger.LogCritical("Bot name is not set in the configuration.");
            return null;
        }

        var botAccount = await _twitchDbService.GetBotAccountAsync(_botSettings.BotName, ct);

        if (botAccount is null)
        {
            _logger.LogCritical("Bot account not found in the database. BotName: {botName}", _botSettings.BotName);
            return null;
        }

        if (string.IsNullOrEmpty(botAccount.ClientId) || string.IsNullOrEmpty(botAccount.ClientSecret))
        {
            _logger.LogCritical("Bot account is missing client id or client secret. BotName: {botName}", _botSettings.BotName);
            return null;
        }

        if (string.IsNullOrEmpty(botAccount.AccessToken))
        {
            _logger.LogDebug("Bot account is missing access token from db. Attempting to fetch new token.");
            var missingTokenResp = await _oAuthService.GetAppAuthToken(botAccount.ClientId, botAccount.ClientSecret, ct);

            return missingTokenResp;
        }

        //check if current db token is valid as one at least exists
        try
        {
            //TODO: this would check every time it's called to see if it's valid. could do that, or add some internal padding to check every x interval instead
            var oAuthValidation = await _oAuthService.ValidateOAuthToken(botAccount.AccessToken, ct);

            ArgumentNullException.ThrowIfNull(oAuthValidation, "OAuth validation response is null");

            //if this gets here, build a fake oauthresponse to mimic what twitch would've returned and send it back early
            var earlyResp = new OAuthResponse
            {
                AccessToken = botAccount.AccessToken,
                TokenType = "Bearer",
                Scope = oAuthValidation?.Scopes
            };

            return earlyResp;
        }
        catch (Exception)
        {
            _logger.LogDebug("Bot account access token indicates invalid. Will attempt to fetch a new one");
        }

        _logger.LogDebug("Fetching new app access token from twitch.");
        var resp = await _oAuthService.GetAppAuthToken(botAccount.ClientId, botAccount.ClientSecret, ct);

        if (resp is null || string.IsNullOrEmpty(resp.AccessToken))
        {
            _logger.LogCritical("Failed to fetch new app access token from twitch.");
            return null;
        }

        //store updated details for next access attempt
        botAccount.AccessToken = resp.AccessToken;
        await _twitchDbService.UpdateBotAccountSettingsAsync(botAccount, ct);

        return resp;
    }
}

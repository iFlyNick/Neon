using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public class UserTokenService(ILogger<UserTokenService> logger, IOAuthService oAuthService, ITwitchDbService twitchDbService, IOptions<NeonSettings> botSettings) : IUserTokenService
{
    private readonly ILogger<UserTokenService> _logger = logger;
    private readonly IOAuthService _oAuthService = oAuthService;
    private readonly ITwitchDbService _twitchDbService = twitchDbService;
    private readonly NeonSettings _botSettings = botSettings.Value;

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

    public async Task<OAuthResponse?> GetUserAccountAuthAsync(string? userCode, string? botAccessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userCode))
        {
            _logger.LogInformation("Missing user code for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(botAccessToken))
        {
            _logger.LogInformation("Missing bot access token for Auth.");
            return null;
        }

        if (string.IsNullOrEmpty(_botSettings.BotName))
        {
            _logger.LogInformation("Missing bot name for Auth.");
            return null;
        }

        var botAccount = await _twitchDbService.GetBotAccountAsync(_botSettings.BotName, ct);

        if (botAccount is null)
        {
            _logger.LogInformation("Bot account not found. BotName: {botName}", _botSettings.BotName);
            return null;
        }

        var userAuth = await _oAuthService.GetUserAuthToken(botAccount.ClientId, botAccount.ClientSecret, userCode, botAccount.RedirectUri);

        return userAuth;
    }
}

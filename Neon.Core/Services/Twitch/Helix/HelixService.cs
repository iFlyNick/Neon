using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Models.Twitch.Helix;
using Neon.Core.Services.Http;
using Neon.Core.Services.Twitch.Authentication;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;

namespace Neon.Core.Services.Twitch.Helix;

public class HelixService(ILogger<HelixService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService, IBotTokenService botTokenService) : IHelixService
{
    private readonly ILogger<HelixService> _logger = logger;
    private readonly IHttpService _httpService = httpService;
    private readonly IBotTokenService _botTokenService = botTokenService;
    private readonly TwitchSettings _twitchSettings = twitchSettings.Value;

    public async Task<TwitchUserAccount?> GetUserAccountDetailsAsync(string? broadcasterId, string? appAccessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(appAccessToken))
        {
            _logger.LogError("Invalid parameters received. Unable to fetch user account details!");
            return null;
        }

        var botClientId = await _botTokenService.GetBotClientIdAsync(ct);

        if (string.IsNullOrEmpty(botClientId))
        {
            _logger.LogError("Failed to fetch bot client id from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var userAccountUrl = $"{helixUrl}/users?id={broadcasterId}";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessToken);
        var headers = new Dictionary<string, string>
        {
            { "Client-Id", botClientId }
        };

        var response = await _httpService.GetAsync(userAccountUrl, authHeader, headers, ct);

        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch user account details from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        var helixResponse = JsonConvert.DeserializeObject<HelixResponse>(responseContent);

        if (helixResponse is null || helixResponse.Users is null || helixResponse.Users.Count == 0)
        {
            _logger.LogError("Failed to deserialize Helix response or no data found in response!");
            return null;
        }

        var targetAccount = helixResponse.Users.FirstOrDefault(s => s.Id == broadcasterId);

        if (targetAccount is null)
            return null;

        var accountDateExact = DateTime.ParseExact(string.IsNullOrEmpty(targetAccount.CreatedAt) ? DateTime.UtcNow.ToString() : targetAccount.CreatedAt, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var utcAccountDate = DateTime.SpecifyKind(accountDateExact, DateTimeKind.Utc);

        var twitchUserAccount = new TwitchUserAccount
        {
            BroadcasterId = targetAccount.Id,
            LoginName = targetAccount.Login,
            DisplayName = targetAccount.DisplayName,
            Type = targetAccount.Type,
            BroadcasterType = targetAccount.BroadcasterType,
            ProfileImageUrl = targetAccount.ProfileImageUrl,
            OfflineImageUrl = targetAccount.OfflineImageUrl,
            CreatedAt = utcAccountDate
        };

        return twitchUserAccount;
    }
}

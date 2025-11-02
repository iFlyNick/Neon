using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Models.Twitch.Helix;
using Neon.Core.Models.Twitch.Helix.WebSockets;
using Neon.Core.Services.Http;
using Neon.Core.Services.Twitch.Authentication;
using Newtonsoft.Json;

namespace Neon.Core.Services.Twitch.Helix;

public class HelixService(ILogger<HelixService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService, IAppTokenService appTokenService) : IHelixService
{
    private readonly TwitchSettings _twitchSettings = twitchSettings.Value;

    public async Task<TwitchUserAccount?> GetUserAccountDetailsAsync(string? broadcasterId, string? appAccessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(appAccessToken))
        {
            logger.LogError("Invalid parameters received. Unable to fetch user account details!");
            return null;
        }

        var appClientId = await appTokenService.GetAppClientIdAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var userAccountUrl = $"{helixUrl}/users?id={broadcasterId}";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessToken);
        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        var response = await httpService.GetAsync(userAccountUrl, authHeader, headers, ct);

        if (response is null || !response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch user account details from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);

        var helixResponse = JsonConvert.DeserializeObject<HelixResponse>(responseContent);

        if (helixResponse is null || helixResponse.Users is null || helixResponse.Users.Count == 0)
        {
            logger.LogError("Failed to deserialize Helix response or no data found in response!");
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

    public async Task SendMessageAsUser(string? message, string? userId, string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(broadcasterId))
            return;

        //need to use app access token as the chat bot has user:bot, and the user that authed the app to join their chat has granted channel:bot
        //technically, userId can be anyone who granted user:chat:write but in reality this should only ever be the twitch account for TheNeonBot (NOT the app account, don't confuse them)
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);
        var appAccessResponse = await appTokenService.GetAppAccountAuthAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return;
        }

        if (appAccessResponse is null || string.IsNullOrEmpty(appAccessResponse.AccessToken))
        {
            logger.LogError("Failed to fetch app access token from database!");
            return;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var userAccountUrl = $"{helixUrl}/chat/messages";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessResponse.AccessToken);

        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        var body = new Dictionary<string, string>
        {
            { "broadcaster_id", broadcasterId },
            { "sender_id", userId },
            { "message", message }
        };

        var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await httpService.PostAsync(userAccountUrl, content, MediaTypeNames.Application.Json, authHeader, headers, ct);

        if (response is null || !response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to send message as userId. Status code: {StatusCode} | UserId: {userId}", response?.StatusCode, userId);
            return; //leaving return here incase something is done after this eventually
        }
    }

    public async Task<string?> GetGlobalEmotes(CancellationToken ct = default)
    {
        //need to use app access token to call out to global emote api
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);
        var appAccessResponse = await appTokenService.GetAppAccountAuthAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        if (appAccessResponse is null || string.IsNullOrEmpty(appAccessResponse.AccessToken))
        {
            logger.LogError("Failed to fetch app access token from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var globalEmotesUrl = $"{helixUrl}/chat/emotes/global";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessResponse.AccessToken);

        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        try
        {
            var response = await httpService.GetAsync(globalEmotesUrl, authHeader, headers, ct);

            if (response is null || !response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch global emotes from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch global emotes from Twitch Helix API. Status code: {StatusCode}", ex.Message);
            return null;
        }
    }

    public async Task<string?> GetChannelEmotes(string? broadcasterId, CancellationToken ct = default)
    {
        //need to use app access token to call out to global emote api
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);
        var appAccessResponse = await appTokenService.GetAppAccountAuthAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        if (appAccessResponse is null || string.IsNullOrEmpty(appAccessResponse.AccessToken))
        {
            logger.LogError("Failed to fetch app access token from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var channelEmotesUrl = $"{helixUrl}/chat/emotes?broadcaster_id={broadcasterId}";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessResponse.AccessToken);

        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        try
        {
            var response = await httpService.GetAsync(channelEmotesUrl, authHeader, headers, ct);

            if (response is null || !response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch channel emotes from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch channel emotes from Twitch Helix API. Status code: {StatusCode}", ex.Message);
            return null;
        }
    }

    public async Task<string?> GetGlobalBadges(CancellationToken ct = default)
    {
        //need to use app access token to call out to global badges api
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);
        var appAccessResponse = await appTokenService.GetAppAccountAuthAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        if (appAccessResponse is null || string.IsNullOrEmpty(appAccessResponse.AccessToken))
        {
            logger.LogError("Failed to fetch app access token from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var globalBadgesUrl = $"{helixUrl}/chat/badges/global";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessResponse.AccessToken);

        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        try
        {
            var response = await httpService.GetAsync(globalBadgesUrl, authHeader, headers, ct);

            if (response is null || !response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch global badges from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch global badges from Twitch Helix API. Status code: {StatusCode}", ex.Message);
            return null;
        }
    }
    
    public async Task<string?> GetChannelBadges(string? broadcasterId, CancellationToken ct = default)
    {
        //need to use app access token to call out to channel badge api
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);
        var appAccessResponse = await appTokenService.GetAppAccountAuthAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        if (appAccessResponse is null || string.IsNullOrEmpty(appAccessResponse.AccessToken))
        {
            logger.LogError("Failed to fetch app access token from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var channelBadgesUrl = $"{helixUrl}/chat/badges?broadcaster_id={broadcasterId}";

        var authHeader = new AuthenticationHeaderValue("Bearer", appAccessResponse.AccessToken);

        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        try
        {
            var response = await httpService.GetAsync(channelBadgesUrl, authHeader, headers, ct);

            if (response is null || !response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch channel badges from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch channel badges from Twitch Helix API. Status code: {StatusCode}", ex.Message);
            return null;
        }
    }

    public async Task<List<WebSocketSubscription>?> GetWebSocketSubscriptions(string? userAccessToken,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userAccessToken);
        
        var appClientId = await appTokenService.GetAppClientIdAsync(ct);

        if (string.IsNullOrEmpty(appClientId))
        {
            logger.LogError("Failed to fetch app client id from database!");
            return null;
        }

        var helixUrl = _twitchSettings.HelixApiUrl;
        var subscriptionsUrl = $"{helixUrl}/eventsub/subscriptions";

        var authHeader = new AuthenticationHeaderValue("Bearer", userAccessToken);
        var headers = new Dictionary<string, string>
        {
            { "Client-Id", appClientId }
        };

        var paginateEnabled = true;
        var retList = new List<WebSocketSubscription>();
        
        while (paginateEnabled && !ct.IsCancellationRequested)
        {
            var response = await httpService.GetAsync(subscriptionsUrl, authHeader, headers, ct);
            
            if (response is null || !response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch websocket subscriptions from Twitch Helix API. Status code: {StatusCode}", response?.StatusCode);
                break;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var helixResponse = JsonConvert.DeserializeObject<WebSocketResponse>(responseContent);
            
            logger.LogDebug("Fetched {subscriptionCount} websocket subscriptions from Helix API page.", helixResponse?.Subscriptions?.Count ?? 0);

            if (string.IsNullOrEmpty(helixResponse?.Pagination?.Cursor))
                paginateEnabled = false;
            
            if (helixResponse?.Subscriptions?.Count > 0)
                retList.AddRange(helixResponse.Subscriptions);
            
            subscriptionsUrl = $"{helixUrl}/eventsub/subscriptions?after={helixResponse?.Pagination?.Cursor}";
        }
        
        return retList;
    }
}

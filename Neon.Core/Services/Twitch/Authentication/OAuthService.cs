using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neon.Core.Models.Twitch;
using Neon.Core.Services.Http;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace Neon.Core.Services.Twitch.Authentication;

public class OAuthService(ILogger<OAuthService> logger, IOptions<TwitchSettings> twitchSettings, IHttpService httpService) : IOAuthService
{
    private readonly ILogger<OAuthService> _logger = logger;
    private readonly TwitchSettings twitchSettings = twitchSettings.Value;
    private readonly IHttpService _httpService = httpService;

    public async Task<OAuthValidationResponse?> ValidateOAuthToken(string? authToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(authToken))
        {
            _logger.LogInformation("Invalid OAuth token request. AuthToken: {authToken}", authToken);
            return null;
        }

        var url = twitchSettings.OAuthValidateUrl;
        var authHeader = new AuthenticationHeaderValue("Bearer", authToken);

        try
        {
            var resp = await _httpService.GetAsync(url, authHeader, null, ct);

            if (resp is null)
            {
                _logger.LogError("Failed to validate OAuth token. Response is null.");
                ArgumentNullException.ThrowIfNull(resp);
            }

            var content = await resp.Content.ReadAsStringAsync(ct);

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to validate OAuth token. Status code: {statusCode} | Body: {message}", resp?.StatusCode, content);
                return null;
            }

            var oAuthResp = JsonConvert.DeserializeObject<OAuthValidationResponse>(content);

            return oAuthResp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate OAuth token.");
            throw;
        }
    }

    public async Task<OAuthResponse?> GetAppAuthToken(string? clientId, string? clientSecret, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogInformation("Invalid OAuth token request. ClientId: {clientId} | ClientSecret: {helper}", clientId, string.IsNullOrEmpty(clientSecret) ? "no value passed" : "value passed");
            return null;
        }

        var data = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        ]);

        var url = twitchSettings.OAuthUrl;
        var contentType = "application/x-www-form-urlencoded";

        try
        {
            var resp = await _httpService.PostAsync(url, data, contentType, null, null, ct);

            if (resp is null)
            {
                _logger.LogError("Failed to get OAuth token. Response is null.");
                ArgumentNullException.ThrowIfNull(resp);
            }

            var content = await resp.Content.ReadAsStringAsync(ct);

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to get OAuth token. Status code: {statusCode} | Body: {message}", resp?.StatusCode, content);
                return null;
            }

            var oAuthResp = JsonConvert.DeserializeObject<OAuthResponse>(content);

            return oAuthResp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OAuth token.");
            throw;
        }
    }

    public async Task<OAuthResponse?> GetUserAuthToken(string? clientId, string? clientSecret, string? userCode, string? redirectUri)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(userCode) || string.IsNullOrEmpty(redirectUri))
        {
            _logger.LogInformation("Invalid OAuth token request. ClientId: {clientId} | ClientSecret: omitted | UserCode: omitted | RedirectUri: {redirect}", clientId, redirectUri);
            return null;
        }

        var data = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", userCode),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
        ]);

        var url = twitchSettings.OAuthUrl;
        var contentType = "application/x-www-form-urlencoded";

        try
        {
            var resp = await _httpService.PostAsync(url, data, contentType, null, null);

            if (resp is null)
            {
                _logger.LogError("Failed to get OAuth token. Response is null.");
                ArgumentNullException.ThrowIfNull(resp);
            }

            var content = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                //this hides 401 errors so need to fix this
                _logger.LogError("Failed to get OAuth token. Status code: {statusCode} | Body: {message}", resp?.StatusCode, content);
                return null;
            }

            var oAuthResp = JsonConvert.DeserializeObject<OAuthResponse>(content);

            return oAuthResp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OAuth token.");
            throw;
        }
    }

    public async Task<OAuthResponse?> GetUserAuthTokenFromRefresh(string? clientId, string? clientSecret, string? refeshToken)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refeshToken))
        {
            _logger.LogInformation("Invalid OAuth token request. ClientId: {clientId} | ClientSecret: omitted | RefreshToken: omitted", clientId);
            return null;
        }

        var data = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refeshToken)
        ]);

        var url = twitchSettings.OAuthUrl;
        var contentType = "application/x-www-form-urlencoded";

        try
        {
            var resp = await _httpService.PostAsync(url, data, contentType, null, null);

            if (resp is null)
            {
                _logger.LogError("Failed to get OAuth token from refresh token. Response is null.");
                ArgumentNullException.ThrowIfNull(resp);
            }

            var content = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                //this hides 401 errors so need to fix this
                _logger.LogError("Failed to get OAuth token from refresh token. Status code: {statusCode} | Body: {message}", resp?.StatusCode, content);
                return null;
            }

            var oAuthResp = JsonConvert.DeserializeObject<OAuthResponse>(content);

            return oAuthResp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OAuth token from refresh token.");
            throw;
        }
    }
}

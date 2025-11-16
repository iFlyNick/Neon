using Microsoft.Extensions.Options;
using Neon.Core.Services.Http;
using Neon.Emotes.Api.Models;
using Newtonsoft.Json.Linq;

namespace Neon.Emotes.Api.Services.BetterTtv;

public class BetterTtvService(ILogger<BetterTtvService> logger, IOptions<EmoteProviderSettings> emoteProviderSettings, IHttpService httpService) : IBetterTtvService
{
    private readonly ILogger<BetterTtvService> _logger = logger;
    private readonly EmoteProviderSettings _emoteProviderSettings = emoteProviderSettings.Value;
    private readonly IHttpService _httpService = httpService;

    private const string EmoteProviderName = "BetterTtv";

    public async Task<List<ProviderEmote>?> GetEmotes(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogError("Twitch broadcaster id is null or empty.");
            return null;
        }

        var settings = GetEmoteProviderSettings();

        if (settings is null)
        {
            _logger.LogError("Emote provider settings are null.");
            return null;
        }

        var channelEmoteUrl = $"{settings.BaseUri}/{settings.TwitchEmoteUri?.Replace("{broadcasterId}", broadcasterId)}";

        if (string.IsNullOrEmpty(channelEmoteUrl))
        {
            _logger.LogError("Channel emote URL is null or empty.");
            return null;
        }

        try
        {
            var response = await _httpService.GetAsync(channelEmoteUrl, null, null, ct);

            if (response is null)
            {
                _logger.LogError("Http response is null from SevenTv api.");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Http response from SevenTv api is not successful. Status code: {statusCode}", response.StatusCode);
                return null;
            }

            var respContent = await response.Content.ReadAsStringAsync(ct);

            var emotes = ConvertApiRespToProviderEmotes(respContent, false);

            return emotes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global emotes from SevenTv.");
            return null;
        }
    }

    public async Task<List<ProviderEmote>?> GetGlobalEmotes(CancellationToken ct = default)
    {
        var settings = GetEmoteProviderSettings();

        if (settings is null)
        {
            _logger.LogError("Emote provider settings are null.");
            return null;
        }

        var globalEmoteUrl = $"{settings.BaseUri}/{settings.GlobalEmoteUri}";

        if (string.IsNullOrEmpty(globalEmoteUrl))
        {
            _logger.LogError("Global emote URL is null or empty.");
            return null;
        }

        try
        {
            var response = await _httpService.GetAsync(globalEmoteUrl, null, null, ct);

            if (response is null)
            {
                _logger.LogError("Http response is null from BetterTtv api.");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Http response from BetterTtv api is not successful. Status code: {statusCode}", response.StatusCode);
                return null;
            }

            var respContent = await response.Content.ReadAsStringAsync(ct);

            var emotes = ConvertApiRespToProviderEmotes(respContent, true);

            return emotes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global emotes from SevenTv.");
            return null;
        }
    }

    private EmoteProvider? GetEmoteProviderSettings()
    {
        var settings = _emoteProviderSettings.EmoteProviders?.FirstOrDefault(s => !string.IsNullOrEmpty(s.Name) && s.Name.Equals(EmoteProviderName, StringComparison.OrdinalIgnoreCase));

        if (settings is null)
        {
            _logger.LogError($"Emote provider settings for {EmoteProviderName} not found.");
            return null;
        }

        return settings;
    }

    private List<ProviderEmote>? ConvertApiRespToProviderEmotes(string? httpResp, bool isGlobal)
    {
        if (string.IsNullOrEmpty(httpResp))
        {
            _logger.LogInformation("Api response is null or empty. No emotes to parse back out.");
            return null;
        }

        var emotes = new List<ProviderEmote>();

        var emoteArray = isGlobal ? ConvertApiGlobalEmotesToProviderEmotes(httpResp) : ConvertApiChannelEmotesToProviderEmotes(httpResp);

        if (emoteArray is null || emoteArray.Count == 0)
        {
            _logger.LogInformation("No emotes found from betterttv response parsing");
            return null;
        }

        emotes.AddRange(emoteArray);

        return emotes;
    }

    private List<ProviderEmote>? ConvertApiGlobalEmotesToProviderEmotes(string? httpResp)
    {
        if (string.IsNullOrEmpty(httpResp))
        {
            _logger.LogInformation("Api response is null or empty. No emotes to parse back out.");
            return null;
        }

        var emotes = new List<ProviderEmote>();

        //Parse the response string to extract emotes
        var emoteArray = JArray.Parse(httpResp);

        if (emoteArray is null || emoteArray.Count == 0)
        {
            _logger.LogInformation("No emotes found in the Helix response.");
            return null;
        }

        foreach (var emote in emoteArray)
        {
            var emoteName = emote["code"]?.ToString();
            var emoteId = emote["id"]?.ToString();

            //for now just access the first image
            //TODO: add support for all image sizes
            var emoteImageUrl = $"https://cdn.betterttv.net/emote/{emoteId}/3x.webp";

            if (string.IsNullOrEmpty(emoteName) || string.IsNullOrEmpty(emoteImageUrl))
            {
                _logger.LogWarning("Emote name or image URL is null or empty. Skipping emote.");
                continue;
            }

            var providerEmote = new ProviderEmote
            {
                Name = emoteName,
                ImageUrl = emoteImageUrl,
                Provider = EmoteProviderEnum.BetterTTV
            };

            emotes.Add(providerEmote);
        }

        return emotes;
    }

    private List<ProviderEmote>? ConvertApiChannelEmotesToProviderEmotes(string? httpResp)
    {
        if (string.IsNullOrEmpty(httpResp))
        {
            _logger.LogInformation("Api response is null or empty. No emotes to parse back out.");
            return null;
        }

        var emotes = new List<ProviderEmote>();

        //Parse the response string to extract emotes
        var jObject = JObject.Parse(httpResp);

        var emoteArray = jObject["sharedEmotes"]?.ToObject<List<JObject>>();

        if (emoteArray is null || emoteArray.Count == 0)
        {
            _logger.LogInformation("No emotes found in the Helix response.");
            return null;
        }

        foreach (var emote in emoteArray)
        {
            var emoteName = emote["code"]?.ToString();
            var emoteId = emote["id"]?.ToString();

            //for now just access the first image
            //TODO: add support for all image sizes
            var emoteImageUrl = $"https://cdn.betterttv.net/emote/{emoteId}/3x.webp";

            if (string.IsNullOrEmpty(emoteName) || string.IsNullOrEmpty(emoteImageUrl))
            {
                _logger.LogWarning("Emote name or image URL is null or empty. Skipping emote.");
                continue;
            }

            var providerEmote = new ProviderEmote
            {
                Name = emoteName,
                ImageUrl = emoteImageUrl,
                Provider = EmoteProviderEnum.BetterTTV
            };

            emotes.Add(providerEmote);
        }

        return emotes;

    }
}

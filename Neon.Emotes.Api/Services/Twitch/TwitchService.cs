using Neon.Core.Services.Twitch.Helix;
using Neon.Emotes.Api.Models;
using Newtonsoft.Json.Linq;

namespace Neon.Emotes.Api.Services.Twitch;

public class TwitchService(ILogger<TwitchService> logger, IHelixService helixService) : ITwitchService
{
    private readonly ILogger<TwitchService> _logger = logger;
    private readonly IHelixService _helixService = helixService;

    public async Task<List<ProviderEmote>?> GetGlobalEmotes(CancellationToken ct = default)
    {
        var helixRespString = await _helixService.GetGlobalEmotes(ct);

        var emotes = ConvertApiRespToProviderEmotes(helixRespString);

        return emotes;
    }

    public async Task<List<ProviderEmote>?> GetEmotes(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogError("Twitch broadcaster id is null or empty.");
            return null;
        }

        var helixRespString = await _helixService.GetChannelEmotes(broadcasterId, ct);

        var emotes = ConvertApiRespToProviderEmotes(helixRespString);

        return emotes;
    }

    private List<ProviderEmote>? ConvertApiRespToProviderEmotes(string? helixResp)
    {
        if (string.IsNullOrEmpty(helixResp))
        {
            _logger.LogInformation("Helix response is null or empty. No emotes to parse back out.");
            return null;
        }

        var emotes = new List<ProviderEmote>();

        //Parse the response string to extract emotes
        var jObject = JObject.Parse(helixResp);

        var emoteArray = jObject["data"]?.ToObject<List<JObject>>();

        if (emoteArray is null || emoteArray.Count == 0)
        {
            _logger.LogInformation("No emotes found in the Helix response.");
            return null;
        }

        foreach (var emote in emoteArray)
        {
            var emoteName = emote["name"]?.ToString();
            //for now just access the smallest image
            //TODO: add support for all image sizes
            var emoteImageUrl = emote["images"]?["url_1x"]?.ToString();

            if (string.IsNullOrEmpty(emoteName) || string.IsNullOrEmpty(emoteImageUrl))
            {
                _logger.LogWarning("Emote name or image URL is null or empty. Skipping emote.");
                continue;
            }

            var providerEmote = new ProviderEmote
            {
                Name = emoteName,
                ImageUrl = emoteImageUrl,
                Provider = EmoteProviderEnum.Twitch
            };

            emotes.Add(providerEmote);
        }

        return emotes;
    }
}

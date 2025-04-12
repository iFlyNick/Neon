using Neon.Emotes.Api.Models;
using Neon.Emotes.Api.Services.Abstractions;
using Neon.Emotes.Api.Services.BetterTtv;
using Neon.Emotes.Api.Services.FrankerFaceZ;
using Neon.Emotes.Api.Services.SevenTv;
using Neon.Emotes.Api.Services.Twitch;

namespace Neon.Emotes.Api.Services.Emote;

public class EmoteService(ILogger<EmoteService> logger, IEnumerable<IIntegratedEmoteService> providerServices) : IEmoteService
{
    private readonly ILogger<EmoteService> _logger = logger;
    private readonly IEnumerable<IIntegratedEmoteService> _providerServices = providerServices;

    public async Task PreloadEmotes(string? broadcasterId, List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || emoteProviders is null || emoteProviders.Count == 0)
        {
            _logger.LogWarning("Broadcaster id is null or empty, or emote providers list is null or empty.");
            return;
        }

        //add redis check to ensure global emotes for the provider are preloaded

        var emotes = new List<ProviderEmote>();

        foreach (var provider in emoteProviders)
        {
            switch (provider)
            {
                case EmoteProviderEnum.Twitch:
                    {
                        var service = _providerServices.FirstOrDefault(s => typeof(ITwitchService).IsAssignableFrom(s.GetType()));

                        if (service is null)
                        {
                            _logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.FrankerFaceZ:
                    {
                        var service = _providerServices.FirstOrDefault(s => typeof(IFrankerFaceZService).IsAssignableFrom(s.GetType()));

                        if (service is null)
                        {
                            _logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.BetterTTV:
                    {
                        var service = _providerServices.FirstOrDefault(s => typeof(IBetterTtvService).IsAssignableFrom(s.GetType()));

                        if (service is null)
                        {
                            _logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.SevenTv:
                    {
                        var service = _providerServices.FirstOrDefault(s => typeof(ISevenTvService).IsAssignableFrom(s.GetType()));

                        if (service is null)
                        {
                            _logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                default:
                    _logger.LogWarning("Unknown emote provider: {provider}", provider);
                    continue;
            }
        }

        //cache emotes using redis using broadcaster id as the key value
        _logger.LogDebug("this is where i'd cache to redis...IF I HAD REDIS RUNNING");
    }
}


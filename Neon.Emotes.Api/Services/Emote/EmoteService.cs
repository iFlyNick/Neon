using System.Text.Json;
using Neon.Core.Services.Redis;
using Neon.Emotes.Api.Models;
using Neon.Emotes.Api.Services.Abstractions;
using Neon.Emotes.Api.Services.BetterTtv;
using Neon.Emotes.Api.Services.FrankerFaceZ;
using Neon.Emotes.Api.Services.SevenTv;
using Neon.Emotes.Api.Services.Twitch;
using Newtonsoft.Json;

namespace Neon.Emotes.Api.Services.Emote;

public class EmoteService(ILogger<EmoteService> logger, IEnumerable<IIntegratedEmoteService> providerServices, IRedisService redisService) : IEmoteService
{
    private readonly ILogger<EmoteService> _logger = logger;
    private readonly IEnumerable<IIntegratedEmoteService> _providerServices = providerServices;
    private readonly IRedisService _redisService = redisService;

    private const string _globalEmoteCacheKey = "globalEmotes";

    public async Task PreloadGlobalEmotes(List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default)
    {
        if (emoteProviders is null || emoteProviders.Count == 0)
        {
            _logger.LogWarning("Emote providers list is null or empty for global emote loading.");
            return;
        }

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

                        var globalEmotes = await service.GetGlobalEmotes(ct);

                        if (globalEmotes is null || globalEmotes.Count == 0)
                            continue;

                        emotes.AddRange(globalEmotes);

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

                        var globalEmotes = await service.GetGlobalEmotes(ct);

                        if (globalEmotes is null || globalEmotes.Count == 0)
                            continue;

                        emotes.AddRange(globalEmotes);

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

                        var globalEmotes = await service.GetGlobalEmotes(ct);

                        if (globalEmotes is null || globalEmotes.Count == 0)
                            continue;

                        emotes.AddRange(globalEmotes);

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

                        var globalEmotes = await service.GetGlobalEmotes(ct);

                        if (globalEmotes is null || globalEmotes.Count == 0)
                            continue;

                        emotes.AddRange(globalEmotes);

                        continue;
                    }
                default:
                    _logger.LogWarning("Unknown emote provider: {provider}", provider);
                    continue;
            }
        }

        //cache emotes using redis using broadcaster id as the key value
        if (await _redisService.Exists(_globalEmoteCacheKey, ct))
        {
            _logger.LogInformation("Redis cache key for global emotes already exists. Skipping creation.");
            return;
        }

        _logger.LogInformation("Attempting to create redis cache key for global emotes");

        await _redisService.Create(_globalEmoteCacheKey, JsonConvert.SerializeObject(emotes), TimeSpan.FromHours(1), ct);

        _logger.LogInformation("Redis cache key created for global emotes");
    }

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

        var cacheKey = $"channelEmotes-{broadcasterId}";

        //cache emotes using redis using broadcaster id as the key value
        if (await _redisService.Exists(cacheKey, ct))
        {
            _logger.LogInformation("Redis cache key for broadcaster emotes already exists. Skipping creation.");
            return;
        }

        _logger.LogInformation("Attempting to create redis cache key for broadcaster emotes");

        await _redisService.Create(cacheKey, JsonConvert.SerializeObject(emotes), TimeSpan.FromHours(1), ct);

        _logger.LogInformation("Redis cache key created for broadcaster emotes");
    }
}


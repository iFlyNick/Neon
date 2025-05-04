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
    private const string GlobalEmoteCacheKey = "globalEmotes";

    public async Task PreloadGlobalEmotes(List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default)
    {
        //cache emotes using redis using broadcaster id as the key value
        if (await redisService.Exists(GlobalEmoteCacheKey, ct))
        {
            logger.LogInformation("Redis cache key for global emotes already exists. Skipping creation.");
            return;
        }

        if (emoteProviders is null || emoteProviders.Count == 0)
        {
            logger.LogWarning("Emote providers list is null or empty for global emote loading.");
            return;
        }
        
        await PreloadEmotes(true, null, GlobalEmoteCacheKey, emoteProviders, ct);
    }

    public async Task PreloadEmotes(string? broadcasterId, List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || emoteProviders is null || emoteProviders.Count == 0)
        {
            logger.LogWarning("Broadcaster id is null or empty, or emote providers list is null or empty.");
            return;
        }

        var cacheKey = $"channelEmotes-{broadcasterId}";

        //cache emotes using redis using broadcaster id as the key value
        if (await redisService.Exists(cacheKey, ct))
        {
            logger.LogInformation("Redis cache key for broadcaster emotes already exists. Skipping creation.");
            return;
        }
        
        await PreloadEmotes(false, broadcasterId, cacheKey, emoteProviders, ct);
    }

    public async Task RemoveChannelEmotes(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogWarning("Broadcaster id is null or empty.");
            return;
        }

        var cacheKey = $"channelEmotes-{broadcasterId}";

        //force purge cache for cacheKey
        await redisService.Remove(cacheKey, ct);
        
        logger.LogInformation("Redis cache key removed for broadcaster emotes: {cacheKey}", cacheKey);
    }
    
    public async Task RefreshChannelEmotes(string? broadcasterId, List<EmoteProviderEnum>? emoteProviders,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId) || emoteProviders is null || emoteProviders.Count == 0)
        {
            logger.LogWarning("Broadcaster id is null or empty, or emote providers list is null or empty.");
            return;
        }

        var cacheKey = $"channelEmotes-{broadcasterId}";
        
        //force purge cache for cacheKey
        await redisService.Remove(cacheKey, ct);
        
        logger.LogInformation("Redis cache key removed for broadcaster emotes: {cacheKey}", cacheKey);
        
        await PreloadEmotes(false, broadcasterId, cacheKey, emoteProviders, ct);
    }

    private async Task PreloadEmotes(bool isGlobal, string? broadcasterId, string? cacheKey, List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default)
    {
        if ((!isGlobal && string.IsNullOrEmpty(broadcasterId)) || string.IsNullOrEmpty(cacheKey) || emoteProviders is null || emoteProviders.Count == 0)
        {
            logger.LogWarning("Broadcaster id is null or empty, cache key is null or empty, or emote providers list is null or empty.");
            return;
        }
        
        var emotes = new List<ProviderEmote>();

        foreach (var provider in emoteProviders)
        {
            switch (provider)
            {
                case EmoteProviderEnum.Twitch:
                    {
                        var service = providerServices.FirstOrDefault(s => s is ITwitchService);

                        if (service is null)
                        {
                            logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = isGlobal ? await service.GetGlobalEmotes(ct) : await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.FrankerFaceZ:
                    {
                        var service = providerServices.FirstOrDefault(s => s is IFrankerFaceZService);

                        if (service is null)
                        {
                            logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = isGlobal ? await service.GetGlobalEmotes(ct) : await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.BetterTTV:
                    {
                        var service = providerServices.FirstOrDefault(s => s is IBetterTtvService);

                        if (service is null)
                        {
                            logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = isGlobal ? await service.GetGlobalEmotes(ct) : await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                case EmoteProviderEnum.SevenTv:
                    {
                        var service = providerServices.FirstOrDefault(s => s is ISevenTvService);

                        if (service is null)
                        {
                            logger.LogWarning("Twitch service not found.");
                            continue;
                        }

                        var providerEmotes = isGlobal ? await service.GetGlobalEmotes(ct) : await service.GetEmotes(broadcasterId, ct);

                        if (providerEmotes is null || providerEmotes.Count == 0)
                            continue;

                        emotes.AddRange(providerEmotes);

                        continue;
                    }
                default:
                    logger.LogWarning("Unknown emote provider: {provider}", provider);
                    continue;
            }
        }

        logger.LogInformation("Attempting to create redis cache key for {cacheKey}", cacheKey);

        await redisService.Create(cacheKey, JsonConvert.SerializeObject(emotes), TimeSpan.FromHours(1), ct);

        logger.LogInformation("Redis cache key created for {cacheKey}", cacheKey);
    }
}

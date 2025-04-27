using Neon.Core.Models.Twitch.EventSub;
using Neon.Core.Services.Redis;
using Neon.Core.Services.Twitch.Helix;
using Neon.TwitchMessageService.Models.Badges;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Neon.TwitchMessageService.Services.Twitch.Badges;

public class BadgeService(ILogger<BadgeService> logger, IRedisService redisService, IHelixService helixService) : IBadgeService
{
    public async Task PreloadGlobalBadgesAsync(CancellationToken ct = default)
    {
        //call out to helix api to get the badge json response, then push to redis cache to save
        //check cache first to avoid api call
        var cacheKey = "globalBadges";
        
        var cachedBadges = await redisService.Get(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedBadges))
        {
            logger.LogDebug("Global badges already cached. No need to call Helix API.");
            return;
        }
        
        var helixRespString = await helixService.GetGlobalBadges(ct);
        if (string.IsNullOrEmpty(helixRespString))
        {
            logger.LogDebug("Helix response is null or empty. No global badges to parse back out.");
            return;
        }
        
        var badgeSets = ParseHelixBadgeResponse(helixRespString);
        if (badgeSets is null || badgeSets.Count == 0)
        {
            logger.LogDebug("No global badges found in the Helix response.");
            return;
        }
        
        var badgeSetJson = JsonConvert.SerializeObject(badgeSets);
        
        await redisService.Create(cacheKey, badgeSetJson, TimeSpan.FromHours(1), ct);
    }

    public async Task PreloadChannelBadgesAsync(string? broadcasterId, CancellationToken ct = default)
    {
        //call out to helix api to get the badge json response for single broadcasterId, then push to redis cache to save
        if (string.IsNullOrEmpty(broadcasterId))
            return;
        
        var cacheKey = $"channelBadges-{broadcasterId}";
        
        var cachedBadges = await redisService.Get(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedBadges))
        {
            logger.LogDebug("Channel badges already cached. No need to call Helix API.");
            return;
        }
        
        var helixRespString = await helixService.GetChannelBadges(broadcasterId, ct);
        if (string.IsNullOrEmpty(helixRespString))
        {
            logger.LogDebug("Helix response is null or empty. No channel badges to parse back out.");
            return;
        }
        
        var badgeSets = ParseHelixBadgeResponse(helixRespString);
        if (badgeSets is null || badgeSets.Count == 0)
        {
            logger.LogDebug("No channel badges found in the Helix response.");
            return;
        }
        
        var badgeSetJson = JsonConvert.SerializeObject(badgeSets);
        
        await redisService.Create(cacheKey, badgeSetJson, TimeSpan.FromHours(1), ct);
    }

    public async Task<List<ProviderBadge>?> GetProviderBadgesFromBadges(string? broadcasterId, List<Badge>? badges, CancellationToken ct = default)
    {
        //take in a list of event sub badges and convert to a list of provider badge sets to be returned back to the caller which will hold the badge urls for the requested inputs
        if (string.IsNullOrEmpty(broadcasterId) || badges is null || badges.Count == 0)
            return null;
        
        var allBadges = new List<ProviderBadgeSet>();
        var globalBadges = await redisService.Get("globalBadges", ct);
        var channelBadges = await redisService.Get($"channelBadges-{broadcasterId}", ct);
        
        if (string.IsNullOrEmpty(globalBadges) && string.IsNullOrEmpty(channelBadges))
        {
            logger.LogDebug("No global or channel badges found in the cache.");
            return null;
        }
        
        if (!string.IsNullOrEmpty(globalBadges))
        {
            var globalBadgeSets = JsonConvert.DeserializeObject<List<ProviderBadgeSet>>(globalBadges);
            if (globalBadgeSets is not null && globalBadgeSets.Count > 0)
                allBadges.AddRange(globalBadgeSets);
        }
        
        if (!string.IsNullOrEmpty(channelBadges))
        {
            var channelBadgeSets = JsonConvert.DeserializeObject<List<ProviderBadgeSet>>(channelBadges);
            if (channelBadgeSets is not null && channelBadgeSets.Count > 0)
                allBadges.AddRange(channelBadgeSets);
        }
        
        var retVal = new List<ProviderBadge>();
        
        foreach (var badge in badges)
        {
            var badgeSet = allBadges.FirstOrDefault(s => s.SetId == badge.SetId);

            var providerBadge = badgeSet?.ProviderBadges?.FirstOrDefault(s => s.Id == badge.Id);
            if (providerBadge is null)
                continue;

            retVal.Add(providerBadge);
        }
        
        return retVal;
    }

    private List<ProviderBadgeSet>? ParseHelixBadgeResponse(string? response)
    {
        if (string.IsNullOrEmpty(response))
            return null;
        
        var jObject = JObject.Parse(response);
        
        var badgeDataArray = jObject["data"]?.ToObject<List<JObject>>();
        if (badgeDataArray is null || badgeDataArray.Count == 0)
            return null;
        
        var badgeSets = new List<ProviderBadgeSet>();

        foreach (var badge in badgeDataArray)
        {
            var tBadgeSet = new ProviderBadgeSet
            {
                SetId = badge["set_id"]?.ToString(),
                ProviderBadges = []
            };
            
            var versions = badge["versions"]?.ToObject<List<JObject>>();
            if (versions is null || versions.Count == 0)
                continue;

            foreach (var version in versions)
            {
                var badgeId = version["id"]?.ToString();
                
                //TODO: figure out how to target multiple image sizes other than grabbing just the first
                var badgeUrl = version["image_url_1x"]?.ToString();
                
                if (string.IsNullOrEmpty(badgeUrl) || string.IsNullOrEmpty(badgeId))
                    continue;
                
                var tBadge = new ProviderBadge
                {
                    Id = badgeId,
                    ImageUrl = badgeUrl
                };
                
                tBadgeSet.ProviderBadges.Add(tBadge);
            }
            
            badgeSets.Add(tBadgeSet);
        }
        
        return badgeSets;
    }
}
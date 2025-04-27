using Microsoft.Extensions.Options;
using Neon.Core.Services.Http;
using Neon.Core.Services.Redis;
using Neon.TwitchMessageService.Models;
using Neon.TwitchMessageService.Models.Emotes;
using Neon.Core.Models.Twitch.EventSub;
using Newtonsoft.Json;

namespace Neon.TwitchMessageService.Services.Twitch;

public class TwitchMessageService(ILogger<TwitchMessageService> logger, IHttpService httpService, IRedisService redisService, IOptions<AppBaseConfig> appBaseConfig) : ITwitchMessageService
{
    private readonly AppBaseConfig _appBaseConfig = appBaseConfig.Value ?? throw new ArgumentNullException(nameof(appBaseConfig));
    
    private const string GlobalEmoteCacheKey = "globalEmotes";

    public async Task<ProcessedMessage?> ProcessTwitchMessage(string? message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        //iterate the message contents to parse out emotes and replace with emote urls from redis cache
        /*
         * emotes can be one of the following:
         *    emote from given twitch channel the message is sent in from 7tv, bttv, ffz, or twitch (ex being any emote for iflynick)
         *    emote can be from a different channel, but only specific to twitch emotes (ex being an emote from skyyexvii sent in iflynick chat)
         */

        var twitchMessage = JsonConvert.DeserializeObject<Message>(message);

        if (string.IsNullOrEmpty(twitchMessage?.Payload?.Event?.TwitchMessage?.Text))
        {
            logger.LogError("Full message text is null or empty.");
            return null;
        }

        //call out to emote api to load all provider emotes for given channel id to ensure they're in the cache. this does not include the sender emotes used from a different twitch channel though
        var broadcasterChannelId = twitchMessage.Payload.Event.BroadcasterUserId;
        try
        {
            logger.LogDebug("Preloading emotes for channel id {channelId}", broadcasterChannelId);
            var url = $"{_appBaseConfig.EmoteApi}/api/Emotes/v1/AllChannelEmotes?broadcasterId={broadcasterChannelId}";
            var resp = await httpService.PostAsync(url, null, null, null, null, ct);
            logger.LogDebug("Http request to emote api returned response code: {responseCode}", resp?.StatusCode);
        } catch (Exception ex)
        {
            logger.LogError("Error preloading all channel emotes for channel id {channelId}: {error}", broadcasterChannelId, ex.Message);
        }
        
        var channelId = twitchMessage.Payload.Event.BroadcasterUserId;

        //group this instead for unique ids
        var emoteFragmentChannelIds = twitchMessage.Payload.Event.TwitchMessage.Fragments?.Where(s => s.Emote is not null).Select(s => s.Emote).Where(s => s is not null && !string.IsNullOrEmpty(s.OwnerId)).Select(s => s!.OwnerId).ToList();
        
        logger.LogDebug("Total emote fragment channel id's found in message: {emoteFragmentChannelIds}", emoteFragmentChannelIds?.Count);

        var allEmotes = new List<ProviderEmote>();

        //call out to emote api to preload twitch emotes for any given channelid in the fragment list. Generally used to capture emotes from other twitch channels
        if (emoteFragmentChannelIds is not null && emoteFragmentChannelIds.Count > 0)
        {
            foreach (var emoteChannelId in emoteFragmentChannelIds)
            {
                if (string.IsNullOrEmpty(emoteChannelId))
                    continue;

                try
                {
                    logger.LogDebug("Preloading emotes for channel id {channelId}", emoteChannelId);
                    var url = $"{_appBaseConfig.EmoteApi}/api/Emotes/v1/TwitchChannelEmotes?broadcasterId={emoteChannelId}";
                    var resp = await httpService.PostAsync(url, null, null, null, null, ct);
                    logger.LogDebug("Http request to emote api returned response code: {responseCode}", resp?.StatusCode);
                } catch (Exception ex)
                {
                    logger.LogError("Error preloading emotes for channel id {channelId}: {error}", emoteChannelId, ex.Message);
                    continue;
                }
                
                var customEmoteCacheKey = $"channelEmotes-{emoteChannelId}";

                var customEmoteString = await redisService.Get(customEmoteCacheKey, ct);

                if (string.IsNullOrEmpty(customEmoteString)) 
                    continue;
                
                var customEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(customEmoteString);
                if (customEmotes is not null && customEmotes.Count > 0)
                {
                    allEmotes.AddRange(customEmotes);
                    logger.LogDebug("Redis cache stats: customEmotes count -> {customCount}", customEmotes.Count);
                }
            }
        }

        var globalEmoteString = await redisService.Get(GlobalEmoteCacheKey, ct);
        
        var channelEmoteCacheKey = $"channelEmotes-{channelId}";
        var channelEmoteString = await redisService.Get(channelEmoteCacheKey, ct);
        
        logger.LogDebug("Redis cache stats: globalEmoteString has value -> {global} | channelEmoteString has value -> {channel}", !string.IsNullOrEmpty(globalEmoteString), !string.IsNullOrEmpty(channelEmoteString));

        if (!string.IsNullOrEmpty(globalEmoteString))
        {
            var globalEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(globalEmoteString);
            if (globalEmotes is not null && globalEmotes.Count > 0)
            {
                allEmotes.AddRange(globalEmotes);
                logger.LogDebug("Redis cache stats: globalEmotes count -> {globalCount}", globalEmotes.Count);
            }
        }

        if (!string.IsNullOrEmpty(channelEmoteString))
        {
            var channelEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(channelEmoteString);
            if (channelEmotes is not null && channelEmotes.Count > 0)
            {
                allEmotes.AddRange(channelEmotes);
                logger.LogDebug("Redis cache stats: channelEmotes count -> {globalCount}", channelEmotes.Count);
            }
        }
        
        var tokenizedMessage = twitchMessage.Payload.Event.TwitchMessage.Text.Split(' ');

        var processedMessageParts = new List<string>();

        foreach (var msg in tokenizedMessage)
        {
            if (allEmotes is null || allEmotes.Count == 0)
            {
                processedMessageParts.Add(msg.Trim());
                continue;
            }

            var emoteUrl = allEmotes.FirstOrDefault(s => s.Name == msg.Trim())?.ImageUrl;

            processedMessageParts.Add(!string.IsNullOrEmpty(emoteUrl)
                ? $"<img src=\"{emoteUrl}\" alt=\"{msg.Trim()}\" />"
                : msg.Trim());
        }

        var processedMessage = string.Join(" ", processedMessageParts);

        var retVal = new ProcessedMessage
        {
            Message = processedMessage,
            ChannelName = twitchMessage.Payload.Event.BroadcasterUserName,
            ChatterName = twitchMessage.Payload.Event.ChatterUserName,
            ChatterColor = twitchMessage.Payload.Event.Color
        };

        return retVal;
    }
}

using Microsoft.Extensions.Options;
using Neon.Core.Models;
using Neon.Core.Models.Chatbot;
using Neon.Core.Models.Kafka;
using Neon.Core.Models.Twitch.EventSub;
using Neon.Core.Services.Http;
using Neon.Core.Services.Kafka;
using Neon.Core.Services.Redis;
using Neon.TwitchMessageService.Models;
using Neon.TwitchMessageService.Models.Badges;
using Neon.TwitchMessageService.Models.Emotes;
using Neon.TwitchMessageService.Services.Twitch.Badges;
using Newtonsoft.Json;

namespace Neon.TwitchMessageService.Services.Twitch;

public class TwitchMessageService(ILogger<TwitchMessageService> logger, IHttpService httpService, IRedisService redisService, IKafkaService kafkaService, IOptions<AppBaseConfig> appBaseConfig, IBadgeService badgeService, IOptions<NeonSettings> neonSettings) : ITwitchMessageService
{
    private readonly AppBaseConfig _appBaseConfig = appBaseConfig.Value ?? throw new ArgumentNullException(nameof(appBaseConfig));
    private readonly NeonSettings _neonSettings = neonSettings.Value ?? throw new ArgumentNullException(nameof(neonSettings));
    
    private const string GlobalEmoteCacheKey = "globalEmotes";
    private const string DefaultChatterColor = "#E79A55";
    
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

        var channelId = twitchMessage.Payload.Event.BroadcasterUserId;

        //check if this message is a command, if so, send it to the topic and continue on
        if (_neonSettings.ChatCommandPrefix is not null &&
            twitchMessage.Payload.Event.TwitchMessage.Text.StartsWith(_neonSettings.ChatCommandPrefix ?? '!'))
        {
            var chatbotMessage = new ChatbotMessage
            {
                Message = twitchMessage.Payload.Event.TwitchMessage.Text,
                ChannelName = twitchMessage.Payload.Event.BroadcasterUserName,
                ChannelId = channelId,
                ChatterName = twitchMessage.Payload.Event.ChatterUserName,
                ChatterId = twitchMessage.Payload.Event.ChatterUserId,
                EventType = "message",
                EventMessage = null //this won't be used for chat based commands
            };
            
            await ProduceChatbotMessage(chatbotMessage, ct);
        }
        
        //a bit overkill, but the internal api should be quick and it will precheck the cache anyway
        await PreloadGlobalEmotes(ct);
        
        await PreloadMessageEmotesToCache(twitchMessage, ct);
        var allEmotes = await GetCachedEmotes(twitchMessage, ct);
        
        await badgeService.PreloadGlobalBadgesAsync(ct);
        await badgeService.PreloadChannelBadgesAsync(channelId, ct);

        var messageBadges = twitchMessage.Payload.Event.Badges;
        var providerBadges = await badgeService.GetProviderBadgesFromBadges(channelId, messageBadges, ct);
        
        var processedMessage = GetProcessedMessage(twitchMessage, allEmotes, providerBadges);

        return processedMessage;
    }

    private async Task PreloadGlobalEmotes(CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Preloading global emotes");
            var url = $"{_appBaseConfig.EmoteApi}/api/Emotes/v1/AllGlobalEmotes";
            var resp = await httpService.PostAsync(url, null, null, null, null, ct);
            logger.LogDebug("Http request to emote api returned response code: {responseCode}", resp?.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError("Error preloading global emotes: {error}", ex.Message);
        }
    }
    
    private async Task PreloadMessageEmotesToCache(Message? message, CancellationToken ct = default)
    {
        if (message is null || message.Payload is null || message.Payload.Event is null)
            return;
        
        //call out to emote api to load all provider emotes for given channel id to ensure they're in the cache. this does not include the sender emotes used from a different twitch channel though
        var broadcasterChannelId = message.Payload.Event.BroadcasterUserId;
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

        //group this instead for unique ids
        var emoteFragmentChannelIds = message.Payload.Event.TwitchMessage?.Fragments?.Where(s => s.Emote is not null).Select(s => s.Emote).Where(s => s is not null && !string.IsNullOrEmpty(s.OwnerId)).Select(s => s!.OwnerId).ToList();
        
        logger.LogDebug("Total emote fragment channel id's found in message: {emoteFragmentChannelIds}", emoteFragmentChannelIds?.Count);

        //call out to emote api to preload twitch emotes for any given channelid in the fragment list. Generally used to capture emotes from other twitch channels
        if (emoteFragmentChannelIds is null || emoteFragmentChannelIds.Count == 0)
            return;
        
        foreach (var emoteChannelId in emoteFragmentChannelIds.Where(emoteChannelId => !string.IsNullOrEmpty(emoteChannelId)))
        {
            try
            {
                logger.LogDebug("Preloading emotes for channel id {channelId}", emoteChannelId);
                var url = $"{_appBaseConfig.EmoteApi}/api/Emotes/v1/TwitchChannelEmotes?broadcasterId={emoteChannelId}";
                var resp = await httpService.PostAsync(url, null, null, null, null, ct);
                logger.LogDebug("Http request to emote api returned response code: {responseCode}", resp?.StatusCode);
            } catch (Exception ex)
            {
                logger.LogError("Error preloading emotes for channel id {channelId}: {error}", emoteChannelId, ex.Message);
            }
        }
    }

    private async Task<List<ProviderEmote>?> GetCachedEmotes(Message? message, CancellationToken ct = default)
    {
        if (message is null || message.Payload is null || message.Payload.Event is null)
            return null;
        
        var retVal = new List<ProviderEmote>();
        var channelId = message.Payload.Event.BroadcasterUserId;
        
        var globalEmoteString = await redisService.Get(GlobalEmoteCacheKey, ct);
        
        var channelEmoteCacheKey = $"channelEmotes-{channelId}";
        var channelEmoteString = await redisService.Get(channelEmoteCacheKey, ct);

        //fetch is in priority of what gets loaded to the list first, going to start with channel, then other channels, then global
        if (!string.IsNullOrEmpty(channelEmoteString))
        {
            var channelEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(channelEmoteString);
            if (channelEmotes is not null && channelEmotes.Count > 0)
            {
                retVal.AddRange(channelEmotes);
                logger.LogDebug("Redis cache stats: channelEmotes count -> {channelEmotes}", channelEmotes.Count);
            }
        }
        
        //need to add channel emotes used from other channels to the return list, they should already be cached by this call
        var emoteFragmentChannelIds = message.Payload.Event.TwitchMessage?.Fragments?.Where(s => s.Emote is not null).Select(s => s.Emote).Where(s => s is not null && !string.IsNullOrEmpty(s.OwnerId)).Select(s => s!.OwnerId).ToList();

        var otherChannelEmoteList = new List<string>();
        if (emoteFragmentChannelIds is not null && emoteFragmentChannelIds.Count > 0)
        {
            foreach (var emoteFragmentChannelId in emoteFragmentChannelIds)
            {
                var cacheKey = $"channelEmotes-{emoteFragmentChannelId}";
                var otherChannelEmoteString = await redisService.Get(cacheKey, ct);

                if (string.IsNullOrEmpty(otherChannelEmoteString))
                    continue;

                otherChannelEmoteList.Add(otherChannelEmoteString);
                logger.LogDebug("Redis cache stats: otherChannelEmotes count -> {otherChannelEmotes}",
                    otherChannelEmoteString);
            }
        }

        foreach (var otherChannelEmote in otherChannelEmoteList)
        {
            var tEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(otherChannelEmote);
            if (tEmotes is null || tEmotes.Count == 0)
                continue;
            
            retVal.AddRange(tEmotes);
        }
        
        if (!string.IsNullOrEmpty(globalEmoteString))
        {
            var globalEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(globalEmoteString);
            if (globalEmotes is not null && globalEmotes.Count > 0)
            {
                retVal.AddRange(globalEmotes);
                logger.LogDebug("Redis cache stats: globalEmotes count -> {globalCount}", globalEmotes.Count);
            }
        }

        return retVal;
    }

    private ProcessedMessage? GetProcessedMessage(Message? message, List<ProviderEmote>? emotes, List<ProviderBadge>? providerBadges)
    {
        if (message is null || message.Payload is null || message.Payload.Event is null || message.Payload.Event.TwitchMessage is null || string.IsNullOrEmpty(message.Payload.Event.TwitchMessage.Text))
            return null;

        ProcessedMessage retVal;
        
        if (emotes is null || emotes.Count == 0)
        {
            logger.LogDebug("No emotes provided from cache, returning processed message without iteration.");
            retVal = new ProcessedMessage
            {
                Message = message.Payload.Event.TwitchMessage.Text,
                ChannelName = message.Payload.Event.BroadcasterUserName,
                ChatterName = message.Payload.Event.ChatterUserName,
                ChatterColor = string.IsNullOrEmpty(message.Payload.Event.Color) ? DefaultChatterColor : message.Payload.Event.Color,
                ChatterBadges = providerBadges
            };
            
            return retVal;
        }
        
        var tokenizedMessage = message.Payload.Event.TwitchMessage.Text.Split(' ');

        var processedMessageParts = new List<string>();

        foreach (var msg in tokenizedMessage)
        {
            var emoteUrl = emotes.FirstOrDefault(s => s.Name == msg.Trim())?.ImageUrl;

            processedMessageParts.Add(!string.IsNullOrEmpty(emoteUrl)
                ? $"<img src=\"{emoteUrl}\" alt=\"{msg.Trim()}\" title=\"{msg.Trim()}\" />"
                : msg.Trim());
        }

        var processedMessage = string.Join(" ", processedMessageParts);

        retVal = new ProcessedMessage
        {
            Message = processedMessage,
            ChannelName = message.Payload.Event.BroadcasterUserName,
            ChatterName = message.Payload.Event.ChatterUserName,
            ChatterColor = string.IsNullOrEmpty(message.Payload.Event.Color) ? DefaultChatterColor : message.Payload.Event.Color,
            ChatterBadges = providerBadges
        };

        return retVal;
    }

    private async Task ProduceChatbotMessage(ChatbotMessage? message, CancellationToken ct = default)
    {
        if (message is null)
            return;
        
        var kafkaProducerConfig = GetKafkaProducerConfig();
        
        await kafkaService.ProduceAsync(kafkaProducerConfig, JsonConvert.SerializeObject(message), null, ct);
    }

    private KafkaProducerConfig GetKafkaProducerConfig()
    {
        return new KafkaProducerConfig
        {
            Topic = "twitch-chatbot-messages",
            TargetPartition = "0",
            BootstrapServers = _appBaseConfig.KafkaBootstrapServers
        };
    }
}

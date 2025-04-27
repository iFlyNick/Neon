using Neon.Core.Services.Http;
using Neon.Core.Services.Redis;
using Neon.TwitchMessageService.Models;
using Neon.TwitchMessageService.Models.Emotes;
using Neon.TwitchService.Models.Twitch;
using Newtonsoft.Json;

namespace Neon.TwitchMessageService.Services.Twitch;

public class TwitchMessageService(ILogger<TwitchMessageService> logger, IHttpService httpService, IRedisService redisService) : ITwitchMessageService
{
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

        var channelId = twitchMessage.Payload.Event.BroadcasterUserId;

        //group this instead for unique ids
        var emoteFragmentChannelIds = twitchMessage.Payload.Event.TwitchMessage.Fragments?.Where(s => s.Emote is not null).Select(s => s.Emote).Where(s => s is not null && !string.IsNullOrEmpty(s.OwnerId)).Select(s => s!.OwnerId).ToList();

        var channelEmoteCacheKey = $"channelEmotes-{channelId}";

        var allEmotes = new List<ProviderEmote>();

        //call out to emote api to preload twitch emotes for any given channelid in the fragment list
        if (emoteFragmentChannelIds is not null && emoteFragmentChannelIds.Count > 0)
        {
            foreach (var emoteChannelId in emoteFragmentChannelIds)
            {
                if (string.IsNullOrEmpty(emoteChannelId))
                    continue;

                var url = $"https://localhost:7286/api/Emotes/v1/TwitchChannelEmotes?broadcasterId={channelId}";
                await httpService.PostAsync(url, null, null, null, null, ct);

                var customEmoteCacheKey = $"customEmotes-{emoteChannelId}";

                var customEmoteString = await redisService.Get(customEmoteCacheKey, ct);

                if (string.IsNullOrEmpty(customEmoteString)) 
                    continue;
                
                var customEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(customEmoteString);
                if (customEmotes is not null && customEmotes.Count > 0)
                    allEmotes.AddRange(customEmotes);
            }
        }

        var globalEmoteString = await redisService.Get(GlobalEmoteCacheKey, ct);
        var channelEmoteString = await redisService.Get(channelEmoteCacheKey, ct);

        if (!string.IsNullOrEmpty(globalEmoteString))
        {
            var globalEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(globalEmoteString);
            if (globalEmotes is not null && globalEmotes.Count > 0)
                allEmotes.AddRange(globalEmotes);
        }

        if (!string.IsNullOrEmpty(channelEmoteString))
        {
            var channelEmotes = JsonConvert.DeserializeObject<List<ProviderEmote>>(channelEmoteString);
            if (channelEmotes is not null && channelEmotes.Count > 0)
                allEmotes.AddRange(channelEmotes);
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
            ChatterName = twitchMessage.Payload.Event.ChatterUserName
        };

        return retVal;
    }
}

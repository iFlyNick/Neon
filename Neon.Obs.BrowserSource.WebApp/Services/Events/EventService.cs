using Neon.Core.Models.Twitch.EventSub;
using Neon.Obs.BrowserSource.WebApp.Models;

namespace Neon.Obs.BrowserSource.WebApp.Services.Events;

public class EventService(ILogger<EventService> logger) : IEventService
{
    public TwitchEventMessage? ProcessMessage(Message? message)
    {
        if (message is null || message.MetaData is null)
            return null;

        var eventType = GetStandardEventType(message.MetaData.SubscriptionType);
        
        if (string.IsNullOrEmpty(eventType))
            return null;
        
        var eventMessage = GetStandardEventMessage(eventType, message);
        if (string.IsNullOrEmpty(eventMessage))
        {
            logger.LogDebug("OBS event service did not find matching message for event type {EventType}. Skipping message creation.", eventType);
            return null;
        }

        var eventLevel = GetEventLevel(eventType, message);

        var retVal = new TwitchEventMessage
        {
            EventType = eventType,
            EventMessage = eventMessage,
            EventLevel = eventLevel,
            ChannelName = message.Payload?.Event?.BroadcasterUserName,
            ChannelId = message.Payload?.Event?.BroadcasterUserId,
            ChatterName = message.Payload?.Event?.UserName,
            ChatterId = message.Payload?.Event?.UserId,
        };

        return retVal;
    }

    private static string? GetStandardEventType(string? eventType)
    {
        return eventType?.ToLower() switch
        {
            "channel.follow" => "follow",
            "channel.subscription.gift" => "gift-sub",
            "channel.subscribe" => "sub",
            "channel.subscription.message" => "resub",
            "channel.channel_points_custom_reward_redemption.add" => "reward-redeem",
            "channel.raid" => "raid",
            "channel.bits.use" => "cheer",
            _ => null
        };
    }

    private string? GetStandardEventMessage(string? eventType, Message? message)
    {
        var subTier = message?.Payload?.Event?.Tier;
        var subTierType = subTier switch
        {
            "1000" => "1",
            "2000" => "2",
            "3000" => "3",
            _ => null
        };

        var anonSub = message?.Payload?.Event?.IsAnonymous ?? false;
        var giftSubCount = int.TryParse(message?.Payload?.Event?.Total, out var count) ? count : 0;
        var giftSubCountString = giftSubCount > 1 ? $"{giftSubCount} tier {subTierType} subs" : $"a tier {subTierType} sub";
        var giftSubMessage = anonSub ? $"An anonymous user gifted {giftSubCountString}!" : $"{message?.Payload?.Event?.UserName} gifted {giftSubCountString}!";

        //exclude power up events for users using bits for emotes
        if (message?.Payload?.Event?.Type == "power_up")
        {
            logger.LogDebug("event service skipping power up cheer event for user {UserName}", message?.Payload?.Event?.UserName);
            return null;
        }
        
        return eventType?.ToLower() switch
        {
            "follow" => $"{message?.Payload?.Event?.UserName} just followed!",
            "gift-sub" => giftSubMessage,
            "sub" => $"{message?.Payload?.Event?.UserName} subscribed at tier {subTierType}!",
            "resub" => $"{message?.Payload?.Event?.UserName} resubscribed at tier {subTierType}!",
            "reward-redeem" => $"{message?.Payload?.Event?.UserName} redeemed {message?.Payload?.Event?.Reward?.Title}!",
            "raid" => $"{message?.Payload?.Event?.FromBroadcasterUserName} just raided with {message?.Payload?.Event?.Viewers} viewers!",
            "cheer" => $"{message?.Payload?.Event?.UserName} just cheered {message?.Payload?.Event?.Bits} bits!",
            _ => null
        };
    }

    private static string? GetEventLevel(string? eventType, Message? message)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "raid" => "high",
            "gift-sub" => int.TryParse(message?.Payload?.Event?.Total, out var count) && count >= 5 ? "high" : "medium", 
            "sub" or "resub" => "medium",
            "cheer" => (message?.Payload?.Event?.Bits ?? 0) >= 1000 ? "high" : "medium",
            _ => "small"
        };
    }
}
using Neon.Core.Models.Chatbot;
using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchChatbotService.Services.Events;

public class EventService(ILogger<EventService> logger) : IEventService
{
    public ChatbotMessage? ProcessMessage(Message? message)
    {
        //consume the chat event payload and return a chatbot message. this will parse the event payload to figure out what type of event it is and then generate a standard event message for the consumer to know how to process it
        
        if (message is null || message.MetaData is null)
            return null;

        var eventType = GetStandardEventType(message.MetaData.SubscriptionType);
        
        if (string.IsNullOrEmpty(eventType))
            return null;
        
        var eventMessage = GetStandardEventMessage(eventType, message);

        var retVal = new ChatbotMessage
        {
            ChannelName = message.Payload?.Event?.BroadcasterUserName,
            ChannelId = message.Payload?.Event?.BroadcasterUserId,
            ChatterName = message.Payload?.Event?.UserName,
            ChatterId = message.Payload?.Event?.UserId,
            Message = null, //not set in standard event capture
            EventType = eventType,
            EventMessage = eventMessage
        };

        return retVal;
    }

    private string? GetStandardEventType(string? eventType)
    {
        return eventType?.ToLower() switch
        {
            "channel.follow" => "follow",
            "channel.subscription.gift" => "gift-sub",
            "channel.subscribe" => "sub",
            "channel.subscription.message" => "resub",
            "channel.ad_break.begin" => "ad-begin",
            "channel.channel_points_custom_reward_redemption.add" => "reward-redeem",
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
        
        return eventType?.ToLower() switch
        {
            "follow" => $"{message?.Payload?.Event?.UserName} just followed!",
            "gift-sub" => giftSubMessage,
            "sub" => $"{message?.Payload?.Event?.UserName} just subscribed at tier {subTierType}!",
            "resub" => $"{message?.Payload?.Event?.UserName} just resubscribed at tier {subTierType}!",
            "ad-begin" => $"An ad break has started. Ad length {message?.Payload?.Event?.DurationSeconds} seconds. We'll be back soon!",
            "reward-redeem" => $"{message?.Payload?.Event?.UserName} redeemed {message?.Payload?.Event?.Reward?.Title}!",
            _ => null
        };
    }
}
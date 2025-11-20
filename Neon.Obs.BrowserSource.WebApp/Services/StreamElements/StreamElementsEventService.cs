using Neon.Core.Data.Twitch;
using Neon.Obs.BrowserSource.WebApp.Models.StreamElements;

namespace Neon.Obs.BrowserSource.WebApp.Services.StreamElements;

public class StreamElementsEventService(ILogger<StreamElementsEventService> logger, ITwitchDbService twitchDbService) : IStreamElementsEventService
{
    public async Task<StreamElementsEventMessage?> ProcessMessage(Message? message, CancellationToken ct = default)
    {
        if (message is null || string.IsNullOrEmpty(message.Topic))
        {
            logger.LogDebug("Ignoring unsupported message topic: {topic}", message?.Topic);
            return null;
        }

        var eventRoomId = message.Room;
        var twitchAccount = await twitchDbService.GetTwitchAccountFromStreamElementsChannel(eventRoomId, ct);

        if (twitchAccount is null || string.IsNullOrEmpty(twitchAccount.BroadcasterId) ||
            string.IsNullOrEmpty(twitchAccount.DisplayName))
        {
            logger.LogError("Could not find Twitch account for StreamElements room ID: {roomId}. Skipping message processing.", eventRoomId);
            return null;
        }
        
        var eventType = GetStandardEventType(message.Topic);

        if (string.IsNullOrEmpty(eventType))
        {
            logger.LogDebug("Ignoring unsupported message topic: {topic}", message?.Topic);
            return null;
        }
        
        var eventMessage = GetStandardEventMessage(eventType, message);
        if (string.IsNullOrEmpty(eventMessage))
        {
            logger.LogDebug("OBS event service did not find matching message for event type {EventType}. Skipping message creation.", eventType);
            return null;
        }
        
        var eventLevel = GetEventLevel(eventType);

        var retVal = new StreamElementsEventMessage
        {
            EventType = eventType,
            EventMessage = eventMessage,
            EventLevel = eventLevel,
            ChannelName = twitchAccount.DisplayName,
            ChannelId = twitchAccount.BroadcasterId,
            DonationAmount = message.Data?.Donation?.Amount,
            DonationCurrency = message.Data?.Donation?.Currency,
            DonorName = message.Data?.Donation?.User?.Username
        };

        return retVal;
    }

    private static string? GetStandardEventType(string? topic)
    {
        return topic?.ToLowerInvariant() switch
        {
            "channel.tips.moderation" => "donation",
            _ => null
        };
    }

    private static string? GetStandardEventMessage(string? eventType, Message? message)
    {
        if (string.IsNullOrEmpty(eventType) || message is null)
            return null;

        return eventType?.ToLowerInvariant() switch
        {
            "donation" => $"{message.Data?.Donation?.User?.Username} donated ${message.Data?.Donation?.Amount?.ToString("n2")}!",
            _ => null
        };
    }
    
    private static string? GetEventLevel(string? eventType)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "donation" => "large",
            _ => "small"
        };
    }
}
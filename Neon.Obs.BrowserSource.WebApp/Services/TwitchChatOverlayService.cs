using Neon.Core.Data.Twitch;
using Neon.Core.Models.Twitch;

namespace Neon.Obs.BrowserSource.WebApp.Services;

public class TwitchChatOverlayService(ILogger<TwitchChatOverlayService> logger, ITwitchDbService dbService) : ITwitchChatOverlayService
{
    public async Task<TwitchChatOverlaySettings?> GetTwitchChatOverlaySettingsByBroadcasterIdAndName(
        string? broadcasterId, string? overlayName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogDebug("Broadcaster ID is null or empty for fetch on chat overlay settings!.");
        }
        
        var userSettings = await dbService.GetAllChatOverlaySettingsByBroadcasterId(broadcasterId, ct);

        if (userSettings is null || userSettings.Count == 0)
        {
            logger.LogDebug("No chat overlay settings found for broadcaster ID: {broadcasterId}", broadcasterId);
            return null;
        }
        
        var overlaySettings = userSettings.FirstOrDefault(s => s.OverlayName == overlayName || string.IsNullOrEmpty(overlayName));
        if (overlaySettings is null)
        {
            logger.LogDebug("No chat overlay settings found for broadcaster ID: {broadcasterId} with overlay name: {overlayName}", broadcasterId, overlayName);
            return null;
        }

        var retVal = new TwitchChatOverlaySettings
        {
            BroadcasterId = broadcasterId,
            OverlayName = overlaySettings.OverlayName,
            OverlayUrl = overlaySettings.OverlayUrl,
            ChatStyle = overlaySettings.ChatStyle,
            IgnoreBotMessages = overlaySettings.IgnoreBotMessages,
            IgnoreCommandMessages = overlaySettings.IgnoreCommandMessages,
            UseTwitchBadges = overlaySettings.UseTwitchBadges,
            UseBetterTtvEmotes = overlaySettings.UseBetterTtvEmotes,
            UseSevenTvEmotes = overlaySettings.UseSevenTvEmotes,
            UseFfzEmotes = overlaySettings.UseFfzEmotes,
            ChatDelayMilliseconds = overlaySettings.ChatDelayMilliseconds,
            AlwaysKeepMessages = overlaySettings.AlwaysKeepMessages,
            ChatMessageRemoveDelayMilliseconds = overlaySettings.ChatMessageRemoveDelayMilliseconds,
            FontFamily = overlaySettings.FontFamily,
            FontSize = overlaySettings.FontSize
        };

        return retVal;
    }
}
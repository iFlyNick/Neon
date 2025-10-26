using Neon.Core.Models.Twitch;

namespace Neon.Obs.BrowserSource.WebApp.Services;

public interface ITwitchChatOverlayService
{
    Task<TwitchChatOverlaySettings?> GetTwitchChatOverlaySettingsByBroadcasterIdAndName(
        string? broadcasterId, string? overlayName, CancellationToken ct = default);
}
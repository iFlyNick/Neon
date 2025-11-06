using Neon.Core.Models.Twitch.Helix.WebSockets;
using Neon.TwitchService.Services.WebSockets;

namespace Neon.TwitchService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    IEnumerable<IWebSocketService> GetWebSocketServices();
    Task Subscribe(string? broadcasterId, CancellationToken ct = default);
    Task SubscribeUserToChat(string? chatterId, string? broadcasterId, CancellationToken ct = default);
    Task Unsubscribe(string? broadcasterName, CancellationToken ct = default);

    Task<List<WebSocketSubscription>?> GetSubscriptions(string? userAccessToken, string? sessionId, string? chatUserId, string? broadcasterUserId, CancellationToken ct = default);
}

using Neon.TwitchService.Services.WebSockets;

namespace Neon.TwitchService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    IEnumerable<IWebSocketService> GetWebSocketServices();
    Task Subscribe(string? userName, CancellationToken ct = default);
    Task SubscribeUserToChat(string? userName, string? broadcasterName, CancellationToken ct = default);
    Task Unsubscribe(string? broadcasterName, CancellationToken ct = default);
}

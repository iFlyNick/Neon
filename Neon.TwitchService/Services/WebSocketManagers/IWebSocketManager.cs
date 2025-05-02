namespace Neon.TwitchService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    Task Subscribe(string? broadcasterName, CancellationToken ct = default);
    Task Unsubscribe(string? broadcasterName, CancellationToken ct = default);
    Task SubscribeUserToChat(string? userName, string? broadcasterName, CancellationToken ct = default);
}

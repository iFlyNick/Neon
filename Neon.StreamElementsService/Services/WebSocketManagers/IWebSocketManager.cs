using Neon.Persistence.EntityModels.Twitch;
using Neon.StreamElementsService.Services.WebSockets;

namespace Neon.StreamElementsService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    IList<IWebSocketService> GetWebSocketServices();
    Task Subscribe(string? broadcasterId, string? channelId, string? jwtToken, CancellationToken ct = default);
}
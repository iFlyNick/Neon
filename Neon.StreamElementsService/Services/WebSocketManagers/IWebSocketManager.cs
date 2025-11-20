using Neon.Persistence.EntityModels.Twitch;

namespace Neon.StreamElementsService.Services.WebSocketManagers;

public interface IWebSocketManager
{
    Task Subscribe(string? broadcasterId, string? channelId, string? jwtToken, CancellationToken ct = default);
}
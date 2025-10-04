using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Neon.Obs.BrowserSource.WebApp.Hubs;

public class ChatHub(ILogger<ChatHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, string> ConnectionChannelMap = new();

    public async Task JoinChannel(string? encryptedKey)
    {
        logger.LogInformation("Join channel invoked for {encryptedKey}", encryptedKey);
        
        if (string.IsNullOrEmpty(encryptedKey))
        {
            logger.LogDebug("Received null or empty encrypted key: {encryptedKey}", encryptedKey);
            return;
        }

        if (ConnectionChannelMap.TryGetValue(Context.ConnectionId, out var connectionChannel))
        {
            logger.LogDebug("Connection {connectionId} is already in channel {channel}", Context.ConnectionId, connectionChannel);
            return;
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, encryptedKey);
        
        ConnectionChannelMap[Context.ConnectionId] = encryptedKey;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionChannelMap.TryGetValue(Context.ConnectionId, out var connectionChannel))
        {
            logger.LogDebug("Connection {connectionId} disconnected from channel {channel}", Context.ConnectionId, connectionChannel);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, connectionChannel);
            ConnectionChannelMap.TryRemove(Context.ConnectionId, out _);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
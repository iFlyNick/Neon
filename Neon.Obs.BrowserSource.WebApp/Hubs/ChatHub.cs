using Microsoft.AspNetCore.SignalR;
using Neon.Obs.BrowserSource.WebApp.Models;

namespace Neon.Obs.BrowserSource.WebApp.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(TwitchMessage? message)
    {
        if (message is null || string.IsNullOrEmpty(message.Message))
            return;
        
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}
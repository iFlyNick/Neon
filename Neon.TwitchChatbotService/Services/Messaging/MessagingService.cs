using Microsoft.Extensions.Options;
using Neon.Core.Data.Twitch;
using Neon.Core.Models;
using Neon.Core.Models.Chatbot;
using Neon.Core.Services.Twitch.Helix;

namespace Neon.TwitchChatbotService.Services.Messaging;

public class MessagingService(ILogger<MessagingService> logger, IHelixService helixService, ITwitchDbService dbService, IOptions<NeonSettings> neonSettings) : IMessagingService
{
    private readonly NeonSettings _neonSettings = neonSettings.Value ?? throw new ArgumentNullException(nameof(neonSettings));
    
    public async Task ProcessMessage(ChatbotMessage? message, CancellationToken ct = default)
    {
        if (message is null || string.IsNullOrEmpty(message.EventMessage) || string.IsNullOrEmpty(message.ChannelId))
        {
            logger.LogDebug("Missing message detail to send message back to channel!");
            return;
        }

        if (string.IsNullOrEmpty(_neonSettings.AppName))
        {
            logger.LogDebug("Missing app name to send message back to channel!");
            return;
        }

        var chatbotAccount = await dbService.GetTwitchAccountByBroadcasterName(_neonSettings.AppName, ct);

        if (chatbotAccount is null)
        {
            logger.LogDebug("Failed to fetch chatbot account from db!");
            return;
        }
        
        await helixService.SendMessageAsUser(message.EventMessage, chatbotAccount.BroadcasterId, message.ChannelId, ct);
    }
}
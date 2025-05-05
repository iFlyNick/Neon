using Neon.Core.Models.Chatbot;

namespace Neon.TwitchChatbotService.Services.Messaging;

public interface IMessagingService
{
    Task ProcessMessage(ChatbotMessage? message, CancellationToken ct = default);
}
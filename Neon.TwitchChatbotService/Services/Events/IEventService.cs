using Neon.Core.Models.Chatbot;
using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchChatbotService.Services.Events;

public interface IEventService
{
    ChatbotMessage? ProcessMessage(Message? message);
}
namespace Neon.Core.Models.Chatbot;

public class ChatbotMessage
{
    public string? ChannelName { get; set; }
    public string? ChannelId { get; set; }
    public string? ChatterName { get; set; }
    public string? ChatterId { get; set; }
    public string? Message { get; set; }
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
}
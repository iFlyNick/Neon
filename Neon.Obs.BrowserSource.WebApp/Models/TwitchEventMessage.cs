namespace Neon.Obs.BrowserSource.WebApp.Models;

public class TwitchEventMessage
{
    public string? EventType { get; set; }
    public string? EventMessage { get; set; }
    public string? EventLevel { get; set; }
    public string? ChannelName { get; set; }
    public string? ChannelId { get; set; }
    public string? ChatterName { get; set; }
    public string? ChatterId { get; set; }
}
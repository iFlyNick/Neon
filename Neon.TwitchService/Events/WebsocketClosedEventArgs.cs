namespace Neon.TwitchService.Events;

public class WebsocketClosedEventArgs : EventArgs
{
    public string? SessionId { get; set; }
    public string? Reason { get; set; }
    public DateTime? EventDate { get; set; }
}
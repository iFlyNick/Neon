namespace Neon.StreamElementsService.Events;

public class WebsocketClosedEventArgs : EventArgs
{
    public string? Reason { get; set; }
    public DateTime? EventDate { get; set; }
}
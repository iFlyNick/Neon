namespace Neon.TwitchService.Events;

public class KeepAliveFailureEventArgs : EventArgs
{
    public string? SessionId { get; set; }
    public DateTime? EventDate { get; set; }
}
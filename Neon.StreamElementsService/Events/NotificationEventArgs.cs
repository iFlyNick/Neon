using Neon.StreamElementsService.Models;

namespace Neon.StreamElementsService.Events;

public class NotificationEventArgs : EventArgs
{
    public Message? Message { get; set; }
    public DateTime EventDate { get; set; }
}
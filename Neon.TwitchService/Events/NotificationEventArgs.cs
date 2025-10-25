using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchService.Events;

public class NotificationEventArgs : EventArgs
{
    public Message? Message { get; set; }
    public DateTime? EventDate { get; set; }
}
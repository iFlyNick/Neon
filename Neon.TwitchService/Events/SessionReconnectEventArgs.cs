using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchService.Events;

public class SessionReconnectEventArgs : EventArgs
{
    public Session? Session { get; set; }
    public DateTime? EventDate { get; set; }
}
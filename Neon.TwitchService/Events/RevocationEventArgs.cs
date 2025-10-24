using Neon.Core.Models.Twitch.EventSub;

namespace Neon.TwitchService.Events;

public class RevocationEventArgs : EventArgs
{
    public Subscription? Subscription { get; set; }
    public DateTime? EventDate { get; set; }
}
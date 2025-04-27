namespace Neon.Core.Models.Twitch.EventSub;

public class Payload
{
    public Event? Event { get; set; }
    public Session? Session { get; set; }
    public Subscription? Subscription { get; set; }
}

namespace Neon.TwitchService.Models.Twitch;

public class Payload
{
    public Event? Event { get; set; }
    public Session? Session { get; set; }
    public Subscription? Subscription { get; set; }
}

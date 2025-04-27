namespace Neon.Core.Models.Twitch.EventSub;

public class Message
{
    public MetaData? MetaData { get; set; }
    public Payload? Payload { get; set; }
}

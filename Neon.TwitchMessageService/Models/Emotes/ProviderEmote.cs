namespace Neon.TwitchMessageService.Models.Emotes;

public class ProviderEmote
{
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public EmoteProviderEnum Provider { get; set; }
}

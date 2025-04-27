namespace Neon.Emotes.Api.Models;

public class ProviderEmote
{
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public EmoteProviderEnum Provider { get; set; }
}

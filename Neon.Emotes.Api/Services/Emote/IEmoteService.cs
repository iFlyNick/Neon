using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.Emote;

public interface IEmoteService
{
    Task PreloadEmotes(string? broadcasterId, List<EmoteProviderEnum>? services, CancellationToken ct = default);
}

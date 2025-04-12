using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.Abstractions;

public interface IIntegratedEmoteService
{
    Task<List<ProviderEmote>?> GetEmotes(string? keyId, CancellationToken ct = default);
    Task<List<ProviderEmote>?> GetGlobalEmotes(CancellationToken ct = default);
}

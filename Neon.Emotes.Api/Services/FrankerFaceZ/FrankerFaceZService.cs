using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.FrankerFaceZ;

public class FrankerFaceZService(ILogger<FrankerFaceZService> logger) : IFrankerFaceZService
{
    private readonly ILogger<FrankerFaceZService> _logger = logger;

    public async Task<List<ProviderEmote>?> GetEmotes(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogError("Twitch broadcaster id is null or empty.");
            return null;
        }

        return null;
    }
}

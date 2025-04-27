using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.BetterTtv;

public class BetterTtvService(ILogger<BetterTtvService> logger) : IBetterTtvService
{
    private readonly ILogger<BetterTtvService> _logger = logger;

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

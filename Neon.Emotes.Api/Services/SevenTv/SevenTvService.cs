using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.SevenTv;

public class SevenTvService(ILogger<SevenTvService> logger) : ISevenTvService
{
    private readonly ILogger<SevenTvService> _logger = logger;

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

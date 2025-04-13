using Neon.TwitchMessageService.Models;

namespace Neon.TwitchMessageService.Services.Twitch;

public interface ITwitchMessageService
{
    Task<ProcessedMessage?> ProcessTwitchMessage(string? message, CancellationToken ct = default);
}

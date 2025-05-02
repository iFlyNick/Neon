namespace Neon.TwitchService.Services;

public interface IStartupService
{
    Task SubscribeAllActiveChannels(CancellationToken ct = default);
}
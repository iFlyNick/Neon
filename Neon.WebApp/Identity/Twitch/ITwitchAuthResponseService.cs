using Neon.WebApp.Identity.Models.Twitch;

namespace Neon.WebApp.Identity.Twitch;

public interface ITwitchAuthResponseService
{
    Task HandleResponseAsync(AuthenticationResponse? response, CancellationToken ct = default);
}
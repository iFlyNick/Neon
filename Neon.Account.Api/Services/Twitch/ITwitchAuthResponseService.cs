using Neon.Account.Api.Models.Twitch;

namespace Neon.Account.Api.Services.Twitch;

public interface ITwitchAuthResponseService
{
    Task HandleResponseAsync(AuthenticationResponse? response, CancellationToken ct = default);
}

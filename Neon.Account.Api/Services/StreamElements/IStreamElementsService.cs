using Neon.Account.Api.Models.StreamElements;

namespace Neon.Account.Api.Services.StreamElements;

public interface IStreamElementsService
{
    Task LinkTwitchAccountToStreamElementsAuth(JwtSetupRequest? jwtSetupRequest, CancellationToken cancellationToken = default);
}
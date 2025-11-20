using Neon.WebApp.Identity.Models.StreamElements;

namespace Neon.WebApp.Identity.StreamElements;

public interface IStreamElementsService
{
    Task LinkTwitchAccountToStreamElementsAuth(JwtSetupRequest? jwtSetupRequest, CancellationToken cancellationToken = default);
}
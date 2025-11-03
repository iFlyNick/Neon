namespace Neon.TwitchService.Services.OAuthValidations;

public interface IOAuthValidationService
{
    Task ValidateAllUserTokensAsync(CancellationToken ct = default);
}
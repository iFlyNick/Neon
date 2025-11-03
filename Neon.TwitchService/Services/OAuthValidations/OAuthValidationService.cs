using Neon.Core.Data.Twitch;
using Neon.Core.Services.Twitch.Authentication;

namespace Neon.TwitchService.Services.OAuthValidations;

public class OAuthValidationService(ILogger<OAuthValidationService> logger, ITwitchDbService dbService, IUserTokenService userTokenService) : IOAuthValidationService
{
    public async Task ValidateAllUserTokensAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Starting validation of all user tokens.");
        
        var userIds = await dbService.GetAllSubscribedChannelBroadcasterIds(ct);
        if (userIds is null || userIds.Count == 0)
        {
            logger.LogDebug("No subscribed channel accounts found for validation.");
            return;
        }

        foreach (var userId in userIds)
        {
            logger.LogDebug("Validating user token for BroadcasterId: {BroadcasterId}", userId);
            try
            {
                await userTokenService.EnsureUserTokenValidByBroadcasterId(userId, ct);
                logger.LogDebug("Successfully validated user token for BroadcasterId: {BroadcasterId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating user token for BroadcasterId: {BroadcasterId}", userId);
            }
        }
    }
}
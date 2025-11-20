using Neon.Account.Api.Models.StreamElements;
using Neon.Core.Data.Twitch;

namespace Neon.Account.Api.Services.StreamElements;

public class StreamElementsService(ILogger<StreamElementsService> logger, ITwitchDbService twitchDbService) : IStreamElementsService
{
    public async Task LinkTwitchAccountToStreamElementsAuth(JwtSetupRequest? jwtSetupRequest,
        CancellationToken ct = default)
    {
        if (jwtSetupRequest is null || string.IsNullOrEmpty(jwtSetupRequest.TwitchBroadcasterId) ||
            string.IsNullOrEmpty(jwtSetupRequest.StreamElementsChannelId) ||
            string.IsNullOrEmpty(jwtSetupRequest.JwtToken))
        {
            logger.LogDebug("Missing information to link Twitch account to StreamElements auth.");
            throw new Exception("Missing information to link Twitch account to StreamElements auth.");
        }

        var twitchAccount = await twitchDbService.GetTwitchAccountDetailForStreamElementsAuth(jwtSetupRequest.TwitchBroadcasterId, ct);
        
        if (twitchAccount is null)
        {
            logger.LogDebug("Twitch account not found for broadcaster ID: {broadcasterId}", jwtSetupRequest.TwitchBroadcasterId);
            throw new Exception($"Twitch account not found in db for broadcaster id. {jwtSetupRequest.TwitchBroadcasterId}");
        }
        
        var resp = await twitchDbService.UpsertStreamElementsAuthForTwitchAccount(twitchAccount, jwtSetupRequest.StreamElementsChannelId, jwtSetupRequest.JwtToken, ct);
        
        logger.LogDebug("Linked Twitch account {twitchAccountId} to StreamElements channel {seChannelId}.",
            twitchAccount.TwitchAccountId, jwtSetupRequest.StreamElementsChannelId);
    }
}
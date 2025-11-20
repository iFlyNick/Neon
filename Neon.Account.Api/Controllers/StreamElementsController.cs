using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neon.Account.Api.Models.StreamElements;
using Neon.Account.Api.Services.StreamElements;

namespace Neon.Account.Api.Controllers;

[ApiController]
public class StreamElementsController(ILogger<StreamElementsController> logger, IStreamElementsService seService) : ControllerBase
{
    [HttpPost]
    [Route("api/v1/streamelements/auth")]
    public async Task<IActionResult> OnPost([FromQuery] JwtSetupRequest? jwtSetupRequest, CancellationToken ct = default)
    {
        if (jwtSetupRequest is null || string.IsNullOrEmpty(jwtSetupRequest.TwitchBroadcasterId) ||
            string.IsNullOrEmpty(jwtSetupRequest.StreamElementsChannelId) ||
            string.IsNullOrEmpty(jwtSetupRequest.JwtToken))
        {
            logger.LogDebug("OnPost for streamelements auth is missing expected parameters!");
            return BadRequest("Missing required parameters.");
        }

        try
        {
            await seService.LinkTwitchAccountToStreamElementsAuth(jwtSetupRequest, ct);
            return Accepted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to link Twitch account to StreamElements auth!");
            return StatusCode(500, "Internal server error.");
        }
    }
}
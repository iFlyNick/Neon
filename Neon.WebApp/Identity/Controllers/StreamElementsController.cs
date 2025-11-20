using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neon.WebApp.Identity.Models.StreamElements;
using Neon.WebApp.Identity.StreamElements;

namespace Neon.WebApp.Identity.Controllers;

[Route("auth/streamelements)")]
public class StreamElementsController(ILogger<StreamElementsController> logger, IStreamElementsService seService) : Controller
{
    [AllowAnonymous]
    [HttpPost]
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
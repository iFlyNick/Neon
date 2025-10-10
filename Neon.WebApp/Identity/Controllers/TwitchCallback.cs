using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neon.WebApp.Identity.Models.Twitch;
using Neon.WebApp.Identity.Twitch;

namespace Neon.WebApp.Identity.Controllers;

[Route("auth/twitch)")]
public class TwitchCallback(ILogger<TwitchCallback> logger, ITwitchAuthResponseService authResponseService) : Controller
{
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> GetCallback([FromQuery] AuthenticationResponse? response, CancellationToken ct = default)
    {
        if (response is null || (string.IsNullOrEmpty(response.Code) && string.IsNullOrEmpty(response.Error)))
            return BadRequest();

        await authResponseService.HandleResponseAsync(response, ct);
        
        //generate jwt token
        
        //redirect to dashboard

        return Ok();
    }
}
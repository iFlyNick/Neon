using Microsoft.AspNetCore.Mvc;
using Neon.Account.Api.Models.Twitch;
using Neon.Account.Api.Services.Twitch;

namespace Neon.Account.Api.Controllers;

[ApiController]
public class TwitchAuthenticationController(ILogger<TwitchAuthenticationController> logger, ITwitchAuthResponseService authResponseService) : ControllerBase
{
    private readonly ILogger<TwitchAuthenticationController> _logger = logger;
    private readonly ITwitchAuthResponseService _authResponseService = authResponseService;

    [HttpGet]
    [Route("api/v1/[controller]")]
    public async Task<IActionResult> TwitchAuthenticationGetAsync([FromQuery] AuthenticationResponse? response, CancellationToken ct = default)
    {
        if (response is null || (string.IsNullOrEmpty(response.Code) && string.IsNullOrEmpty(response.Error)))
            return BadRequest();

        await _authResponseService.HandleResponseAsync(response, ct);

        return Ok();
    }
}

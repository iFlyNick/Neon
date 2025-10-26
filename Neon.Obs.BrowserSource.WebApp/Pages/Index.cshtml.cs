using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Neon.Obs.BrowserSource.WebApp.Services;

namespace Neon.Obs.BrowserSource.WebApp.Pages;

public class IndexModel(ILogger<IndexModel> logger, ITwitchChatOverlayService overlayService) : PageModel
{
    public void OnGet([FromQuery] string? broadcasterId)
    {
        logger.LogInformation("Index page accessed with broadcasterId: {broadcasterId}", broadcasterId);
    }

    public async Task<IActionResult> OnGetChatOverlaySettings(string? broadcasterId, string? overlayName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
            return BadRequest("Missing broadcaster id parameter.");

        var settings =
            await overlayService.GetTwitchChatOverlaySettingsByBroadcasterIdAndName(broadcasterId, overlayName, ct);

        return new JsonResult(settings);
    }
}
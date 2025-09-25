using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Neon.Obs.BrowserSource.WebApp.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    public void OnGet([FromQuery] string? broadcasterId)
    {
        logger.LogInformation("Index page accessed with broadcasterId: {broadcasterId}", broadcasterId);
    }
}
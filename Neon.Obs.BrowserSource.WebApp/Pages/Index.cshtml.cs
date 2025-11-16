using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Neon.Obs.BrowserSource.WebApp.Services;

namespace Neon.Obs.BrowserSource.WebApp.Pages;

public class IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment env, ITwitchChatOverlayService overlayService) : PageModel
{
    public string? BroadcasterId { get; set; }
    public string? OverlayType { get; set; }
    public List<string>? CustomUsers { get; set; }
    public List<string>? BotUsers { get; set; }
    public bool? IgnoreCommandMessages { get; set; }
    
    public void OnGet([FromQuery] string? broadcasterId)
    {
        logger.LogInformation("Index page accessed with broadcasterId: {broadcasterId}", broadcasterId);
        BroadcasterId = broadcasterId;

        GetCustomUsers();
        GetBotUsers();
        IgnoreCommandMessages = true;
        OverlayType = "skyye";
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

    private void GetCustomUsers()
    {
        var rootPath = Path.Combine(env.WebRootPath, "images", "skyye");

        if (!Directory.Exists(rootPath)) return;

        foreach (var folderPath in Directory.EnumerateDirectories(rootPath))
        {
            var folderName = Path.GetFileName(folderPath);

            CustomUsers ??= [];
            CustomUsers.Add(folderName);
        }
    }

    private void GetBotUsers()
    {
        List<string> knownBotUsers = ["theneonbot", "streamelements", "streamlabs"];
        BotUsers = knownBotUsers;
    }
}
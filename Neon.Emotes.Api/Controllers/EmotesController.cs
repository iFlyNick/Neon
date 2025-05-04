using Microsoft.AspNetCore.Mvc;
using Neon.Emotes.Api.Models;
using Neon.Emotes.Api.Services.Emote;

namespace Neon.Emotes.Api.Controllers;

[Route("api/[controller]/v1")]
[ApiController]
public class EmotesController(ILogger<EmotesController> logger, IEmoteService emoteService) : ControllerBase
{
    [HttpPost]
    [Route("AllGlobalEmotes")]
    public async Task<IActionResult> AllGlobalEmotesPostAsync(CancellationToken ct = default)
    {
        //want to try and fetch from all services, so list all enums
        var emoteProviders = new List<EmoteProviderEnum>
        {
            EmoteProviderEnum.Twitch,
            EmoteProviderEnum.SevenTv,
            EmoteProviderEnum.BetterTTV,
            EmoteProviderEnum.FrankerFaceZ
        };

        try
        {
            await emoteService.PreloadGlobalEmotes(emoteProviders, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error preloading global emotes");
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }

    [HttpPost]
    [Route("AllChannelEmotes")]
    public async Task<IActionResult> AllChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogError("Broadcaster id is null or empty.");
            return BadRequest("Broadcaster id is null or empty.");
        }

        //want to try and fetch from all services, so list all enums
        var emoteProviders = new List<EmoteProviderEnum>
        {
            EmoteProviderEnum.Twitch,
            EmoteProviderEnum.SevenTv,
            EmoteProviderEnum.BetterTTV,
            EmoteProviderEnum.FrankerFaceZ
        };

        try
        {
            await emoteService.PreloadEmotes(broadcasterId, emoteProviders, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error preloading emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }
    
    [HttpPost]
    [Route("RefreshChannelEmotes")]
    public async Task<IActionResult> RefreshChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogError("Broadcaster id is null or empty.");
            return BadRequest("Broadcaster id is null or empty.");
        }

        //want to try and fetch from all services, so list all enums
        var emoteProviders = new List<EmoteProviderEnum>
        {
            EmoteProviderEnum.Twitch,
            EmoteProviderEnum.SevenTv,
            EmoteProviderEnum.BetterTTV,
            EmoteProviderEnum.FrankerFaceZ
        };

        try
        {
            await emoteService.RefreshChannelEmotes(broadcasterId, emoteProviders, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }
    
    [HttpPost]
    [Route("RemoveChannelEmotes")]
    public async Task<IActionResult> RemoveChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogError("Broadcaster id is null or empty.");
            return BadRequest("Broadcaster id is null or empty.");
        }

        try
        {
            await emoteService.RemoveChannelEmotes(broadcasterId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }

    [HttpPost]
    [Route("TwitchChannelEmotes")]
    public async Task<IActionResult> TwitchChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            logger.LogError("Broadcaster id is null or empty.");
            return BadRequest("Broadcaster id is null or empty.");
        }

        var emoteProviders = new List<EmoteProviderEnum>
        {
            EmoteProviderEnum.Twitch
        };

        try
        {
            await emoteService.PreloadEmotes(broadcasterId, emoteProviders, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error preloading twitch emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }
}

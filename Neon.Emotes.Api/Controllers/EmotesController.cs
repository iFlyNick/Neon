using Microsoft.AspNetCore.Mvc;
using Neon.Emotes.Api.Models;
using Neon.Emotes.Api.Services.Emote;

namespace Neon.Emotes.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmotesController(ILogger<EmotesController> logger, IEmoteService emoteService) : ControllerBase
{
    private readonly ILogger<EmotesController> _logger = logger;
    private readonly IEmoteService _emoteService = emoteService;

    [HttpPost]
    [Route("v1/AllGlobalEmotes")]
    /// <summary>
    /// Triggers request to initiate emote cache loading for all global emotes across integrated platforms. Used typically at startup to precache global emotes
    /// This will fetch for example all Twitch Global Emotes, 7TV, FFZ, BTTV, etc emotes and then sending them to the caching engine.
    /// </summary>

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
            await _emoteService.PreloadGlobalEmotes(emoteProviders, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading global emotes");
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }

    [HttpPost]
    [Route("v1/AllChannelEmotes")]
    /// <summary>
    /// Triggers request to initiate emote cache loading for all emotes across integrated platforms for a given broadcaster. Used typically if the user has authorized the bot to interact with their channel.
    /// This will fetch for example all Twitch Channel Emotes, 7TV, FFZ, BTTV, etc emotes using their broadcaster id as the hook, and then sending them to the caching engine.
    /// </summary>

    public async Task<IActionResult> AllChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogError("Broadcaster id is null or empty.");
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
            await _emoteService.PreloadEmotes(broadcasterId, emoteProviders, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }

    [HttpPost]
    [Route("v1/TwitchChannelEmotes")]
    /// <summary>
    /// Triggers request to initiate emote cache loading for only twitch specific channel emotes for a given broadcaster. Typically used for 'random' emotes sent in chat from a different broadcaster that would still want to display on consuming services.
    /// </summary>
    public async Task<IActionResult> TwitchChannelEmotesPostAsync(string? broadcasterId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger.LogError("Broadcaster id is null or empty.");
            return BadRequest("Broadcaster id is null or empty.");
        }

        var emoteProviders = new List<EmoteProviderEnum>
        {
            EmoteProviderEnum.Twitch
        };

        try
        {
            await _emoteService.PreloadEmotes(broadcasterId, emoteProviders, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading twitch emotes for broadcaster {BroadcasterId}", broadcasterId);
            return StatusCode(500, "Internal server error");
        }

        return Ok();
    }
}

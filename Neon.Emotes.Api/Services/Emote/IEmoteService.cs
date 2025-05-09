﻿using Neon.Emotes.Api.Models;

namespace Neon.Emotes.Api.Services.Emote;

public interface IEmoteService
{
    Task PreloadGlobalEmotes(List<EmoteProviderEnum>? emoteProviders, CancellationToken ct = default);
    Task PreloadEmotes(string? broadcasterId, List<EmoteProviderEnum>? services, CancellationToken ct = default);
    Task RefreshChannelEmotes(string? broadcasterId, List<EmoteProviderEnum>? services, CancellationToken ct = default);
    Task RemoveChannelEmotes(string? broadcasterId, CancellationToken ct = default);
}

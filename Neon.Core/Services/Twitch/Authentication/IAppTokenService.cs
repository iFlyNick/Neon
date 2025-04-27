﻿using Neon.Core.Models.Twitch;

namespace Neon.Core.Services.Twitch.Authentication;

public interface IAppTokenService
{
    Task<string?> GetAppClientIdAsync(CancellationToken ct = default);
    Task<OAuthResponse?> GetAppAccountAuthAsync(CancellationToken ct = default);
}

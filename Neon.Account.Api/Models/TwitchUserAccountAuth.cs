using Neon.Account.Api.Models.Twitch;
using Neon.Core.Models.Twitch;

namespace Neon.Account.Api.Models;

public class TwitchUserAccountAuth
{
    public AuthenticationResponse? AuthenticationResponse { get; set; }
    public OAuthResponse? OAuthResponse { get; set; }
    public OAuthValidationResponse? OAuthValidationResponse { get; set; }
    public TwitchUserAccount? TwitchUserAccount { get; set; }
}

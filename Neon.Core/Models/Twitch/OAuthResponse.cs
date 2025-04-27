using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch;

public class OAuthResponse
{
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }
    public List<string>? Scope { get; set; }
    [JsonProperty("token_type")]
    public string? TokenType { get; set; }
}

using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch;

public class OAuthValidationResponse
{
    [JsonProperty("client_id")]
    public string? ClientId { get; set; }
    [JsonProperty("login")]
    public string? Login { get; set; }
    [JsonProperty("user_id")]
    public string? UserId { get; set; }
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonProperty("scopes")]
    public List<string>? Scopes { get; set; }
}

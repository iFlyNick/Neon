using Newtonsoft.Json;

namespace Neon.Account.Api.Models.Twitch;

public class AuthenticationResponse
{
    [JsonProperty("code")]
    public string? Code { get; set; }
    [JsonProperty("error")]
    public string? Error { get; set; }
    [JsonProperty("error_description")]
    public string? ErrorDescription { get; set; }
}

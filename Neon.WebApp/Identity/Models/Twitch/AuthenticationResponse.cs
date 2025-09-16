using System.Text.Json.Serialization;

namespace Neon.WebApp.Identity.Models.Twitch;

public class AuthenticationResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
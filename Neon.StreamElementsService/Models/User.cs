using Newtonsoft.Json;

namespace Neon.StreamElementsService.Models;

public class User
{
    [JsonProperty("username")]
    public string? Username { get; set; }
    [JsonProperty("geo")]
    public string? Geo { get; set; }
    // [JsonProperty("email")]
    // public string? Email { get; set; }
    [JsonProperty("channel")]
    public string? Channel { get; set; }
}
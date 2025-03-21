using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.Helix;

public class HelixResponse
{
    [JsonProperty("data")]
    public List<UserAccount>? Users { get; set; }
}

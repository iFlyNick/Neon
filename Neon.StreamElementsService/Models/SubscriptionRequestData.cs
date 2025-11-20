using Newtonsoft.Json;

namespace Neon.StreamElementsService.Models;

public class SubscriptionRequestData
{
    [JsonProperty("topic")]
    public string? Topic { get; set; }
    [JsonProperty("room")]
    public string? Room { get; set; }
    [JsonProperty("token")]
    public string? Token { get; set; }
    [JsonProperty("token_type")]
    public string? TokenType { get; set; }
}
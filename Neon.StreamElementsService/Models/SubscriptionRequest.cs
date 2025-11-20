using Newtonsoft.Json;

namespace Neon.StreamElementsService.Models;

public class SubscriptionRequest
{
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("nonce")]
    public string? Nonce { get; set; }
    [JsonProperty("data")]
    public SubscriptionRequestData? Data { get; set; }
}
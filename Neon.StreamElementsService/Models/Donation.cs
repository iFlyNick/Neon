using Newtonsoft.Json;

namespace Neon.StreamElementsService.Models;

public class Donation
{
    [JsonProperty("user")]
    public User? User { get; set; }
    [JsonProperty("message")]
    public string? Message { get; set; }
    [JsonProperty("amount")]
    public double? Amount { get; set; }
    [JsonProperty("currency")]
    public string? Currency { get; set; }
    [JsonProperty("paymentMethod")]
    public string? PaymentMethod { get; set; }
}
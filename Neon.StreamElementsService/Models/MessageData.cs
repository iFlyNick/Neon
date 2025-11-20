using Newtonsoft.Json;

namespace Neon.StreamElementsService.Models;

public class MessageData
{
    [JsonProperty("donation")]
    public Donation? Donation { get; set; }
    [JsonProperty("_id")]
    public string? Id { get; set; }
    [JsonProperty("channel")]
    public string? Channel { get; set; } //this is the channel id of the person receiving the donation
    [JsonProperty("provider")]
    public string? Provider { get; set; }
    [JsonProperty("approved")]
    public string? ApprovalStatus { get; set; }
    [JsonProperty("status")]
    public string? Status { get; set; }
    [JsonProperty("createdAt")]
    public string? CreatedAt { get; set; }
    [JsonProperty("updatedAt")]
    public string? UpdatedAt { get; set; }
    [JsonProperty("transactionId")]
    public string? TransactionId { get; set; }
}
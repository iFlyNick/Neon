using Newtonsoft.Json;

namespace Neon.Obs.BrowserSource.WebApp.Models.StreamElements;

public class Message
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("ts")]
    public string? TimeStamp { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("topic")]
    public string? Topic { get; set; }
    [JsonProperty("room")]
    public string? Room { get; set; }
    [JsonProperty("data")]
    public MessageData? Data { get; set; }
}
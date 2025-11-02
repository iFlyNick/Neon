using System.Text.Json.Serialization;

namespace Neon.Core.Models.Twitch.EventSub;

public class Pagination
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}
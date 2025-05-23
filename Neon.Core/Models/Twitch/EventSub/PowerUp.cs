﻿using Newtonsoft.Json;

namespace Neon.Core.Models.Twitch.EventSub;

public class PowerUp
{
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("emote")]
    public Emote? Emote { get; set; }
    [JsonProperty("message_effect_id")]
    public string? MessageEffectId { get; set; }
}

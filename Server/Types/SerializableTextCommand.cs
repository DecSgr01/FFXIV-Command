using System;
using Newtonsoft.Json;

namespace Command.Server.Types; 

[Serializable]
public class SerializableTextCommand {
    [JsonProperty("command")] public string? Command { get; set; }

    [JsonProperty("__dangerDoNotTouch!EnableSafeMode")]
    public bool SafeMode = true;
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmokeScreenEngine
{
    public class SyncResponse
    {
        [JsonPropertyName("keys")]
        public List<SyncKey>? Keys { get; set; }
    }

    public class SyncKey
    {
        [JsonPropertyName("key_value")]
        public string KeyValue { get; set; } = "";

        [JsonPropertyName("duration_type")]
        public string DurationType { get; set; } = "";

        [JsonPropertyName("duration_ms")]
        public long DurationMs { get; set; }
    }
}

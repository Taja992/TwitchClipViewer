using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchClipViewer.Models
{
    public class ClipsResponse
    {
        [JsonPropertyName("data")]
        public List<Clip>? Data { get; set; }

        [JsonPropertyName("pagination")]
        public Pagination? Pagination { get; set; }
    }

    public class Pagination
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }
}
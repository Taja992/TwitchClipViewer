using System;
using System.Text.Json.Serialization;

namespace TwitchClipViewer.Models
{
    public class Clip
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("embed_url")]
        public string? EmbedUrl { get; set; }

        [JsonPropertyName("broadcaster_id")]
        public string? BroadcasterId { get; set; }

        [JsonPropertyName("broadcaster_name")]
        public string? BroadcasterName { get; set; }

        [JsonPropertyName("creator_id")]
        public string? CreatorId { get; set; }

        [JsonPropertyName("creator_name")]
        public string? CreatorName { get; set; }

        [JsonPropertyName("video_id")]
        public string? VideoId { get; set; }

        [JsonPropertyName("game_id")]
        public string? GameId { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("view_count")]
        public int? ViewCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        [JsonPropertyName("is_featured")]
        public bool? IsFeatured { get; set; }
    }
}
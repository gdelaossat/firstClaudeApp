using System.Text.Json.Serialization;

namespace firstClaudeApp.Models;

/// <summary>
/// Represents a Substack chat message. Field names match the actual API response
/// as documented by the Substack Chat Exporter Chrome extension.
/// </summary>
public class SubstackMessage
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("body_json")]
    public object? BodyJson { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("user_handle")]
    public string? UserHandle { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("user_photo_url")]
    public string? UserPhotoUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("publication_id")]
    public long PublicationId { get; set; }

    [JsonPropertyName("link_url")]
    public string? LinkUrl { get; set; }

    [JsonPropertyName("audience")]
    public string? Audience { get; set; }

    [JsonPropertyName("comment_count")]
    public int CommentCount { get; set; }

    [JsonPropertyName("reaction_count")]
    public int ReactionCount { get; set; }

    // Backward-compatible accessors for the service layer
    [JsonIgnore]
    public string Name => UserName;

    [JsonIgnore]
    public DateTime Timestamp => CreatedAt;
}

public class SubstackChatChannel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("publication_id")]
    public long PublicationId { get; set; }

    [JsonPropertyName("publication_name")]
    public string PublicationName { get; set; } = string.Empty;

    [JsonPropertyName("channel_type")]
    public string ChannelType { get; set; } = string.Empty;
}

public class SubstackChatResponse
{
    [JsonPropertyName("messages")]
    public List<SubstackMessage> Messages { get; set; } = new();

    [JsonPropertyName("more")]
    public bool More { get; set; }
}

public class SubstackChannelsResponse
{
    [JsonPropertyName("channels")]
    public List<SubstackChatChannel> Channels { get; set; } = new();
}

public class ChatSummary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChannelName { get; set; } = string.Empty;
    public long ChannelId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalMessages { get; set; }
    public string AiSummary { get; set; } = string.Empty;
    public ChatStatistics Statistics { get; set; } = new();
}

public class ChatStatistics
{
    public int TotalMessages { get; set; }
    public int UniqueParticipants { get; set; }
    public Dictionary<string, int> MessagesByUser { get; set; } = new();
    public Dictionary<string, int> MessagesByHour { get; set; } = new();
    public List<string> MostActiveUsers { get; set; } = new();
}

public class SubstackSettings
{
    public string SessionCookie { get; set; } = string.Empty;
    public string PublicationSubdomain { get; set; } = "pobremillenial";
    public string AnthropicApiKey { get; set; } = string.Empty;
    public int SummaryIntervalHours { get; set; } = 24;
    public int MaxMessagesToFetch { get; set; } = 500;
    public bool AutoSummaryEnabled { get; set; } = false;
}

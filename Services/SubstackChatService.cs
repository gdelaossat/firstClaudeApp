using System.Net;
using System.Text.Json;
using firstClaudeApp.Models;

namespace firstClaudeApp.Services;

/// <summary>
/// Service to fetch chat messages from Substack's internal API.
///
/// NOTE: Substack does not have an official public API for chat. These endpoints
/// are based on reverse-engineering. The exact URLs may need adjustment once you
/// inspect real traffic via browser DevTools (Network tab → XHR filter while on
/// https://substack.com/chat). Auth uses the connect.sid session cookie.
/// </summary>
public class SubstackChatService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;
    private readonly ILogger<SubstackChatService> _logger;
    private const string BaseUrl = "https://substack.com/api/v1";

    public SubstackChatService(HttpClient httpClient, SettingsService settingsService, ILogger<SubstackChatService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    private void ConfigureRequest(HttpRequestMessage request)
    {
        var settings = _settingsService.GetSettings();
        // Substack uses connect.sid for authentication; some libraries also reference substack.sid
        request.Headers.Add("Cookie", $"connect.sid={settings.SessionCookie}; substack.sid={settings.SessionCookie}");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        request.Headers.Add("Accept", "application/json");
    }

    public async Task<List<SubstackChatChannel>> GetChatChannelsAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/reader/chat_channels");
            ConfigureRequest(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SubstackChannelsResponse>(json);
            return result?.Channels ?? new List<SubstackChatChannel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch chat channels from Substack");
            return new List<SubstackChatChannel>();
        }
    }

    public async Task<List<SubstackChatChannel>> GetChannelsForPublicationAsync(string subdomain)
    {
        var allChannels = await GetChatChannelsAsync();
        return allChannels
            .Where(c => c.PublicationName.Contains(subdomain, StringComparison.OrdinalIgnoreCase)
                     || c.ChannelType == "default")
            .ToList();
    }

    public async Task<(List<SubstackMessage> Messages, bool HasMore)> GetMessagesAsync(long channelId, long? beforeId = null, int limit = 50)
    {
        try
        {
            var url = $"{BaseUrl}/chat/channel/{channelId}/messages?limit={limit}";
            if (beforeId.HasValue)
                url += $"&before={beforeId.Value}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            ConfigureRequest(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SubstackChatResponse>(json);

            return (result?.Messages ?? new List<SubstackMessage>(), result?.More ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch messages for channel {ChannelId}", channelId);
            return (new List<SubstackMessage>(), false);
        }
    }

    public async Task<List<SubstackMessage>> GetAllRecentMessagesAsync(long channelId, int maxMessages = 500, DateTime? since = null)
    {
        var allMessages = new List<SubstackMessage>();
        long? beforeId = null;
        var settings = _settingsService.GetSettings();
        var maxToFetch = Math.Min(maxMessages, settings.MaxMessagesToFetch);

        while (allMessages.Count < maxToFetch)
        {
            var batchSize = Math.Min(50, maxToFetch - allMessages.Count);
            var (messages, hasMore) = await GetMessagesAsync(channelId, beforeId, batchSize);

            if (messages.Count == 0)
                break;

            if (since.HasValue)
            {
                var filtered = messages.Where(m => m.Timestamp >= since.Value).ToList();
                allMessages.AddRange(filtered);

                if (filtered.Count < messages.Count)
                    break; // We've gone past the since date
            }
            else
            {
                allMessages.AddRange(messages);
            }

            if (!hasMore)
                break;

            beforeId = messages.Last().Id;
            await Task.Delay(200); // Rate limiting
        }

        return allMessages.OrderBy(m => m.Timestamp).ToList();
    }

    public ChatStatistics CalculateStatistics(List<SubstackMessage> messages)
    {
        var stats = new ChatStatistics
        {
            TotalMessages = messages.Count,
            UniqueParticipants = messages.Select(m => m.Name).Distinct().Count(),
            MessagesByUser = messages
                .GroupBy(m => m.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            MessagesByHour = messages
                .GroupBy(m => m.Timestamp.Hour.ToString("D2") + ":00")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count()),
            MostActiveUsers = messages
                .GroupBy(m => m.Name)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key} ({g.Count()} msgs)")
                .ToList()
        };

        return stats;
    }
}

using firstClaudeApp.Models;

namespace firstClaudeApp.Services;

public class ChatSummaryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatSummaryBackgroundService> _logger;
    private readonly SummaryStore _summaryStore;
    private readonly SettingsService _settingsService;

    public ChatSummaryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ChatSummaryBackgroundService> logger,
        SummaryStore summaryStore,
        SettingsService settingsService)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _summaryStore = summaryStore;
        _settingsService = settingsService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chat summary background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _settingsService.GetSettings();
            var delay = TimeSpan.FromHours(Math.Max(1, settings.SummaryIntervalHours));

            await Task.Delay(delay, stoppingToken);

            if (!settings.AutoSummaryEnabled || !_settingsService.IsConfigured)
                continue;

            try
            {
                _logger.LogInformation("Running automatic chat summary");
                await GenerateSummaryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automatic summary generation");
            }
        }
    }

    private async Task GenerateSummaryAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var substackService = scope.ServiceProvider.GetRequiredService<SubstackChatService>();
        var claudeService = scope.ServiceProvider.GetRequiredService<ClaudeSummaryService>();
        var settings = _settingsService.GetSettings();

        var channels = await substackService.GetChannelsForPublicationAsync(settings.PublicationSubdomain);

        foreach (var channel in channels)
        {
            if (ct.IsCancellationRequested) break;

            var since = DateTime.UtcNow.AddHours(-settings.SummaryIntervalHours);
            var messages = await substackService.GetAllRecentMessagesAsync(channel.Id, settings.MaxMessagesToFetch, since);

            if (messages.Count == 0)
                continue;

            var aiSummary = await claudeService.SummarizeMessagesAsync(messages, channel.Name);
            var statistics = substackService.CalculateStatistics(messages);

            var summary = new ChatSummary
            {
                ChannelName = channel.Name,
                ChannelId = channel.Id,
                PeriodStart = since,
                PeriodEnd = DateTime.UtcNow,
                TotalMessages = messages.Count,
                AiSummary = aiSummary,
                Statistics = statistics
            };

            _summaryStore.AddSummary(summary);
            _logger.LogInformation("Auto-summary generated for channel {Channel}: {Count} messages", channel.Name, messages.Count);
        }
    }
}

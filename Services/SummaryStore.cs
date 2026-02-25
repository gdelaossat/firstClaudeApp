using firstClaudeApp.Models;

namespace firstClaudeApp.Services;

public class SummaryStore
{
    private readonly List<ChatSummary> _summaries = new();
    private readonly Lock _lock = new();

    public event Action? OnSummariesChanged;

    public void AddSummary(ChatSummary summary)
    {
        lock (_lock)
        {
            _summaries.Insert(0, summary);
            // Keep last 50 summaries
            if (_summaries.Count > 50)
                _summaries.RemoveRange(50, _summaries.Count - 50);
        }
        OnSummariesChanged?.Invoke();
    }

    public List<ChatSummary> GetSummaries()
    {
        lock (_lock)
        {
            return _summaries.ToList();
        }
    }

    public ChatSummary? GetLatestSummary()
    {
        lock (_lock)
        {
            return _summaries.FirstOrDefault();
        }
    }
}

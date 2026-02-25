using firstClaudeApp.Models;

namespace firstClaudeApp.Services;

public class SettingsService
{
    private SubstackSettings _settings = new();
    public event Action? OnSettingsChanged;

    public SubstackSettings GetSettings() => _settings;

    public void UpdateSettings(SubstackSettings settings)
    {
        _settings = settings;
        OnSettingsChanged?.Invoke();
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.SessionCookie) &&
        !string.IsNullOrWhiteSpace(_settings.AnthropicApiKey);
}

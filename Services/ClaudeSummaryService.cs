using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using firstClaudeApp.Models;

namespace firstClaudeApp.Services;

public class ClaudeSummaryService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;
    private readonly ILogger<ClaudeSummaryService> _logger;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const int MaxTokensPerChunk = 80000; // ~20K words, well within context window

    public ClaudeSummaryService(HttpClient httpClient, SettingsService settingsService, ILogger<ClaudeSummaryService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<string> SummarizeMessagesAsync(List<SubstackMessage> messages, string channelName)
    {
        if (messages.Count == 0)
            return "No hay mensajes para resumir.";

        var chunks = ChunkMessages(messages);

        if (chunks.Count == 1)
        {
            return await SummarizeChunkAsync(chunks[0], channelName, isFinalSummary: true);
        }

        // Multi-chunk: summarize each chunk, then create a final summary
        var chunkSummaries = new List<string>();
        for (int i = 0; i < chunks.Count; i++)
        {
            _logger.LogInformation("Summarizing chunk {Current}/{Total}", i + 1, chunks.Count);
            var chunkSummary = await SummarizeChunkAsync(chunks[i], channelName, isFinalSummary: false);
            chunkSummaries.Add(chunkSummary);
        }

        return await CombineSummariesAsync(chunkSummaries, channelName);
    }

    private List<string> ChunkMessages(List<SubstackMessage> messages)
    {
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var estimatedTokens = 0;

        foreach (var msg in messages)
        {
            var line = $"[{msg.Timestamp:yyyy-MM-dd HH:mm}] {msg.Name}: {msg.Body}\n";
            var lineTokens = line.Length / 4; // rough token estimate

            if (estimatedTokens + lineTokens > MaxTokensPerChunk && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
                estimatedTokens = 0;
            }

            currentChunk.Append(line);
            estimatedTokens += lineTokens;
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString());

        return chunks;
    }

    private async Task<string> SummarizeChunkAsync(string messagesText, string channelName, bool isFinalSummary)
    {
        var systemPrompt = isFinalSummary
            ? """
              Eres un asistente que resume conversaciones de chats de Substack en español.
              Genera un resumen claro y bien organizado con:
              1. **Temas principales** discutidos
              2. **Puntos clave** y opiniones destacadas
              3. **Debates o discusiones** relevantes
              4. **Links o recursos** compartidos (si los hay)
              5. **Conclusiones** o consensos alcanzados
              Usa formato Markdown. Sé conciso pero completo.
              """
            : """
              Resume brevemente los temas principales y puntos clave de este fragmento de conversación de chat de Substack.
              Sé conciso. Usa español.
              """;

        var userPrompt = isFinalSummary
            ? $"Resume la siguiente conversación del chat '{channelName}':\n\n{messagesText}"
            : $"Resume este fragmento del chat '{channelName}':\n\n{messagesText}";

        return await CallClaudeApiAsync(systemPrompt, userPrompt);
    }

    private async Task<string> CombineSummariesAsync(List<string> chunkSummaries, string channelName)
    {
        var combined = string.Join("\n\n---\n\n", chunkSummaries.Select((s, i) => $"Parte {i + 1}:\n{s}"));

        var systemPrompt = """
            Eres un asistente que crea resúmenes finales combinando resúmenes parciales de un chat de Substack.
            Genera un resumen unificado, bien organizado con:
            1. **Temas principales** discutidos
            2. **Puntos clave** y opiniones destacadas
            3. **Debates o discusiones** relevantes
            4. **Links o recursos** compartidos (si los hay)
            5. **Conclusiones** o consensos alcanzados
            Elimina redundancias y unifica la información. Usa formato Markdown y español.
            """;

        var userPrompt = $"Combina estos resúmenes parciales del chat '{channelName}' en un solo resumen final:\n\n{combined}";

        return await CallClaudeApiAsync(systemPrompt, userPrompt);
    }

    private async Task<string> CallClaudeApiAsync(string systemPrompt, string userPrompt)
    {
        var settings = _settingsService.GetSettings();

        if (string.IsNullOrWhiteSpace(settings.AnthropicApiKey))
            return "Error: API key de Anthropic no configurada.";

        var requestBody = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 4096,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        request.Headers.Add("x-api-key", settings.AnthropicApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode} - {Body}", response.StatusCode, responseJson);
                return $"Error al llamar a Claude API: {response.StatusCode}";
            }

            var result = JsonSerializer.Deserialize<ClaudeApiResponse>(responseJson);
            return result?.Content?.FirstOrDefault()?.Text ?? "No se pudo generar el resumen.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Claude API");
            return $"Error: {ex.Message}";
        }
    }
}

file class ClaudeApiResponse
{
    [JsonPropertyName("content")]
    public List<ClaudeContentBlock>? Content { get; set; }
}

file class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

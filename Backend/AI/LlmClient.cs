using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartMeeting.Api.AI;

public interface ILlmClient
{
    bool IsConfigured { get; }

    /// <summary>Appelle le LLM en imposant un format JSON (Structured Outputs).</summary>
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, string jsonSchemaName, string jsonSchema,
        CancellationToken cancellationToken = default);

    /// <summary>Appelle le LLM pour obtenir un texte libre (explications, procès-verbal).</summary>
    Task<string> CompleteTextAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}

public class LlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<LlmClient> _logger;

    public LlmClient(HttpClient httpClient, LlmSettings settings, ILogger<LlmClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

        if (settings.IsConfigured)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }
    }

    public bool IsConfigured => _settings.IsConfigured;

    public Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, string jsonSchemaName,
        string jsonSchema, CancellationToken cancellationToken = default)
    {
        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = jsonSchemaName,
                strict = true,
                schema = JsonSerializer.Deserialize<JsonElement>(jsonSchema)
            }
        };

        return SendAsync(systemPrompt, userPrompt, responseFormat, cancellationToken);
    }

    public Task<string> CompleteTextAsync(string systemPrompt, string userPrompt,
        CancellationToken cancellationToken = default)
        => SendAsync(systemPrompt, userPrompt, null, cancellationToken);

    private async Task<string> SendAsync(string systemPrompt, string userPrompt, object? responseFormat,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Aucune clé API LLM configurée (variable LLM_API_KEY).");
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _settings.Model,
            ["temperature"] = 0.1,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        if (responseFormat is not null)
        {
            payload["response_format"] = responseFormat;
        }

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_settings.BaseUrl.TrimEnd('/')}/chat/completions",
            content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Erreur LLM {Status} : {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Le service IA a renvoyé une erreur ({(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(body);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}

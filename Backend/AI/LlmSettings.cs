namespace SmartMeeting.Api.AI;

public class LlmSettings
{
    /// <summary>URL compatible OpenAI (OpenAI, Groq, Ollama, Azure...).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 60;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

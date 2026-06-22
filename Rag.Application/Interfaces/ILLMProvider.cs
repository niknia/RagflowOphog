namespace Rag.Application.Interfaces;

public class ChatResult
{
    public string Content { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}

public interface ILLMProvider
{
    string Name { get; }
    Task<ChatResult> GenerateChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}

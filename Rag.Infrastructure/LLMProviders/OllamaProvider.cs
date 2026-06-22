using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Infrastructure.Configuration;
using Rag.Shared.Extensions;

namespace Rag.Infrastructure.LLMProviders;

public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly OllamaConfiguration _config;

    public string Name => "Ollama";

    public OllamaProvider(HttpClient httpClient, OllamaConfiguration config, ILogger<OllamaProvider> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatResult> GenerateChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var request = new
        {
            model = _config.ChatModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = false
        };
        var response = await _httpClient.PostAsJsonAsync("/api/chat", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
        return new ChatResult
        {
            Content = result?.Message?.Content ?? string.Empty,
            PromptTokens = result?.PromptEvalCount ?? 0,
            CompletionTokens = result?.EvalCount ?? 0
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = new { model = _config.EmbeddingModel, prompt = text };
            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: ct);
            if (result?.Embedding == null || result.Embedding.Length == 0)
                throw new InvalidOperationException("Empty embedding response");
            return result.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama embedding failed");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var tasks = texts.Select(t => GenerateEmbeddingAsync(t, ct));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
        public int PromptEvalCount { get; set; }
        public int EvalCount { get; set; }
    }

    private class OllamaMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Infrastructure.Configuration;

namespace Rag.Infrastructure.LLMProviders;

public class XinferenceProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XinferenceProvider> _logger;
    private readonly XinferenceConfiguration _config;

    public string Name => "Xinference";

    public XinferenceProvider(HttpClient httpClient, XinferenceConfiguration config, ILogger<XinferenceProvider> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ChatResult> GenerateChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var request = new
        {
            model = _config.ModelUid,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = false
        };
        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(cancellationToken: ct);
        return new ChatResult
        {
            Content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
            PromptTokens = result?.Usage?.PromptTokens ?? 0,
            CompletionTokens = result?.Usage?.CompletionTokens ?? 0
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                model = _config.ModelUid,
                input = text
            };
            var response = await _httpClient.PostAsJsonAsync("/v1/embeddings", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(cancellationToken: ct);
            if (result?.Data == null || result.Data.Count == 0)
                throw new InvalidOperationException("Empty embedding response");
            return result.Data[0].Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xinference embedding failed");
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
            var response = await _httpClient.GetAsync("/v1/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private class OpenAIChatResponse
    {
        public List<OpenAIChoice>? Choices { get; set; }
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
    }

    private class OpenAIMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAIUsage
    {
        [System.Text.Json.Serialization.JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private class OpenAIEmbeddingResponse
    {
        public List<OpenAIEmbeddingData>? Data { get; set; }
    }

    private class OpenAIEmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

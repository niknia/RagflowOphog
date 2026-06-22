using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Rag.Infrastructure.Configuration;

namespace Rag.Infrastructure.LLMProviders;

public class XinferenceChatService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly XinferenceConfiguration _config;
    private readonly ILogger<XinferenceChatService> _logger;
    private readonly Dictionary<string, object?> _attributes = new();

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public XinferenceChatService(HttpClient httpClient, XinferenceConfiguration config, ILogger<XinferenceChatService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _attributes["ModelId"] = config.ChatModelUid;
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var messages = chatHistory.Select(m => new
        {
            role = m.Role.ToString().ToLowerInvariant(),
            content = m.Content
        }).ToList();

        var request = new
        {
            model = _config.ChatModelUid,
            messages,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<XinferenceChatResponse>(cancellationToken: cancellationToken);

        var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        return new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, content)
        };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatHistory.Select(m => new
        {
            role = m.Role.ToString().ToLowerInvariant(),
            content = m.Content
        }).ToList();

        var request = new
        {
            model = _config.ChatModelUid,
            messages,
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions") { Content = content };
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
            var data = line[6..];
            if (data == "[DONE]") yield break;

            XinferenceChatChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<XinferenceChatChunk>(data);
            }
            catch { continue; }

            var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, delta);
        }
    }

    private class XinferenceChatResponse
    {
        public List<XinferenceChoice>? Choices { get; set; }
    }

    private class XinferenceChoice
    {
        public XinferenceMessage? Message { get; set; }
        public XinferenceDelta? Delta { get; set; }
    }

    private class XinferenceMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private class XinferenceDelta
    {
        public string Content { get; set; } = string.Empty;
    }

    private class XinferenceChatChunk
    {
        public List<XinferenceChoice>? Choices { get; set; }
    }
}

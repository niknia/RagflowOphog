using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Rag.Application.Interfaces;
using Rag.Application.SemanticKernel.Factories;
using Rag.Domain.Enums;
using Rag.Domain.ValueObjects;
using Rag.Domain.Entities;

namespace Rag.Application.Services;
public class ChatService
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IRetrievalService _retrievalService;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<ChatService> _logger;
    private readonly IOllamaConfiguration _config;

    public ChatService(
        IKernelFactory kernelFactory,
        IRetrievalService retrievalService,
        IChatRepository chatRepository,
        IOllamaConfiguration config,
        ILogger<ChatService> logger)
    {
        _kernelFactory = kernelFactory;
        _retrievalService = retrievalService;
        _chatRepository = chatRepository;
        _logger = logger;
        _config = config;
    }

    public async Task<(string Response, List<Citation> Citations)> SendMessageAsync(
        Guid chatId, string message, RetrievalMode mode = RetrievalMode.Hybrid, string collectionId = "default", CancellationToken ct = default)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat == null) throw new InvalidOperationException("Chat not found");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var kernel = _kernelFactory.CreateChatKernel(chat.Model);
        var options = new RetrievalOptions { Mode = mode, CollectionId = collectionId };
        var searchResults = await _retrievalService.RetrieveAsync(message, options, ct);

        var context = BuildContext(searchResults);
        var citations = BuildCitations(searchResults);
        var history = await BuildHistoryAsync(chatId, ct);

        var prompt = $"""
            You are a helpful AI assistant. Answer the user's question based on the provided context.
            If the context doesn't contain enough information, say so.
            Provide specific references to the source documents when possible.

            Context:
            {context}

            Conversation History:
            {history}

            User: {message}
            Assistant:
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);
        sw.Stop();

        var responseText = result.ToString();
        await SaveMessageAsync(chatId, "user", message, mode, citations, ct);
        await SaveMessageAsync(chatId, "assistant", responseText, mode, citations, ct);

        _logger.LogInformation("Chat {ChatId} response in {Elapsed}ms", chatId, sw.ElapsedMilliseconds);
        return (responseText, citations);
    }

    private static string BuildContext(IReadOnlyList<SearchResult> results)
    {
        var sb = new StringBuilder();
        foreach (var r in results.Take(5))
        {
            sb.AppendLine($"[Source: {r.DocumentName}, Page: {r.PageNumber}, Chunk: {r.ChunkIndex}, Score: {r.HybridScore:F2}]");
            sb.AppendLine(r.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static List<Citation> BuildCitations(IReadOnlyList<SearchResult> results)
    {
        return results.Take(5).Select(r => new Citation
        {
            DocumentName = r.DocumentName,
            Collection = r.CollectionName,
            PageNumber = r.PageNumber,
            ChunkNumber = r.ChunkIndex,
            RetrievalSource = r.RetrievalSource,
            Score = r.HybridScore > 0 ? r.HybridScore : Math.Max(r.VectorScore, r.KeywordScore),
            SearchType = r.SearchType,
            Content = r.Content.Length > 200 ? r.Content[..200] + "..." : r.Content
        }).ToList();
    }

    private async Task<string> BuildHistoryAsync(Guid chatId, CancellationToken ct)
    {
        var messages = await _chatRepository.GetMessagesAsync(chatId, ct);
        var sb = new StringBuilder();
        foreach (var m in messages.TakeLast(10))
            sb.AppendLine($"{m.Role}: {m.Content[..Math.Min(m.Content.Length, 500)]}");
        return sb.ToString();
    }

    private async Task SaveMessageAsync(Guid chatId, string role, string content, RetrievalMode mode, List<Citation> citations, CancellationToken ct)
    {
        var msg = new ChatMessage
        {
            ChatId = chatId,
            Role = role,
            Content = content,
            RetrievalMode = mode,
            Citations = System.Text.Json.JsonSerializer.Serialize(citations)
        };
        await _chatRepository.AddMessageAsync(msg, ct);
    }
}

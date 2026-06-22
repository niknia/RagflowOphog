using System.ComponentModel;
using Microsoft.SemanticKernel;
using Rag.Application.Interfaces;
using Rag.Domain.ValueObjects;

namespace Rag.Application.SemanticKernel.Plugins;
public class RetrievalPlugin
{
    private readonly IRetrievalService _retrievalService;

    public RetrievalPlugin(IRetrievalService retrievalService) => _retrievalService = retrievalService;

    [KernelFunction("search_documents")]
    [Description("Search documents using hybrid retrieval combining vector and keyword search")]
    public async Task<string> SearchDocumentsAsync(
        [Description("The search query")] string query,
        [Description("Collection ID to search in")] string collectionId = "default",
        [Description("Number of results")] int limit = 5,
        CancellationToken ct = default)
    {
        var options = new RetrievalOptions
        {
            Mode = Rag.Domain.Enums.RetrievalMode.Hybrid,
            CollectionId = collectionId,
            FinalLimit = limit
        };
        var results = await _retrievalService.RetrieveAsync(query, options, ct);
        var output = string.Join("\n\n", results.Select(r =>
            $"[Document: {r.DocumentName}, Page: {r.PageNumber}, Score: {r.HybridScore:F2}]\n{r.Content}"));
        return output;
    }

    [KernelFunction("search_vector")]
    [Description("Search documents using vector similarity only")]
    public async Task<string> SearchVectorAsync(
        [Description("The search query")] string query,
        [Description("Collection ID")] string collectionId = "default",
        int limit = 5,
        CancellationToken ct = default)
    {
        var options = new RetrievalOptions
        {
            Mode = Rag.Domain.Enums.RetrievalMode.VectorOnly,
            CollectionId = collectionId,
            FinalLimit = limit
        };
        var results = await _retrievalService.RetrieveAsync(query, options, ct);
        return string.Join("\n\n", results.Select(r =>
            $"[Document: {r.DocumentName}, Score: {r.VectorScore:F2}]\n{r.Content}"));
    }

    [KernelFunction("search_keyword")]
    [Description("Search documents using keyword/BM25 search only")]
    public async Task<string> SearchKeywordAsync(
        [Description("The search query")] string query,
        [Description("Collection ID")] string collectionId = "default",
        int limit = 5,
        CancellationToken ct = default)
    {
        var options = new RetrievalOptions
        {
            Mode = Rag.Domain.Enums.RetrievalMode.KeywordOnly,
            CollectionId = collectionId,
            FinalLimit = limit
        };
        var results = await _retrievalService.RetrieveAsync(query, options, ct);
        return string.Join("\n\n", results.Select(r =>
            $"[Document: {r.DocumentName}, Score: {r.KeywordScore:F2}]\n{r.Content}"));
    }
}

using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;
using Rag.Domain.Enums;
using Rag.Domain.ValueObjects;
using Rag.Shared.Extensions;

namespace Rag.Application.Services;
public class HybridRetrievalService : IRetrievalService
{
    private readonly IVectorStore _vectorStore;
    private readonly ISearchEngine _searchEngine;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRerankingService _rerankingService;
    private readonly ILogger<HybridRetrievalService> _logger;

    public HybridRetrievalService(
        IVectorStore vectorStore,
        ISearchEngine searchEngine,
        IEmbeddingService embeddingService,
        IRerankingService rerankingService,
        ILogger<HybridRetrievalService> logger)
    {
        _vectorStore = vectorStore;
        _searchEngine = searchEngine;
        _embeddingService = embeddingService;
        _rerankingService = rerankingService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> RetrieveAsync(string query, RetrievalOptions options, CancellationToken ct = default)
    {
        return options.Mode switch
        {
            RetrievalMode.VectorOnly => await VectorSearchAsync(query, options, ct),
            RetrievalMode.KeywordOnly => await KeywordSearchAsync(query, options, ct),
            RetrievalMode.Hybrid => await HybridSearchAsync(query, options, ct),
            _ => await HybridSearchAsync(query, options, ct)
        };
    }

    public async Task<IReadOnlyList<SearchResult>> VectorSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        var results = await _vectorStore.SearchAsync(options.CollectionId, embedding, options.VectorLimit, ct);
        return results.Select(r => new SearchResult
        {
            Id = r.Id,
            VectorScore = r.Score,
            SearchType = "Vector",
            RetrievalSource = "VectorDatabase"
        }).ToList();
    }

    public async Task<IReadOnlyList<SearchResult>> KeywordSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default)
    {
        var results = await _searchEngine.SearchAsync("knowledge-index", query, options.KeywordLimit, ct);
        return results.Select(r => new SearchResult
        {
            Id = r.Id,
            Content = r.Content,
            KeywordScore = r.Score,
            SearchType = "Keyword",
            RetrievalSource = "SearchEngine",
            DocumentId = r.Metadata.GetValueOrDefault("DocumentId")?.ToString() ?? "",
            ChunkIndex = int.Parse(r.Metadata.GetValueOrDefault("ChunkIndex")?.ToString() ?? "0"),
            PageNumber = int.Parse(r.Metadata.GetValueOrDefault("PageNumber")?.ToString() ?? "0")
        }).ToList();
    }

    public async Task<IReadOnlyList<SearchResult>> HybridSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        var vectorTask = _vectorStore.SearchAsync(options.CollectionId, embedding, options.VectorLimit, ct);
        var keywordTask = _searchEngine.SearchAsync("knowledge-index", query, options.KeywordLimit, ct);
        await Task.WhenAll(vectorTask, keywordTask);

        var vectorResults = vectorTask.Result;
        var keywordResults = keywordTask.Result;

        var merged = new Dictionary<string, SearchResult>();
        int idx = 0;
        foreach (var vr in vectorResults)
        {
            merged[vr.Id] = new SearchResult
            {
                Id = vr.Id,
                VectorScore = vr.Score,
                KeywordScore = 0,
                SearchType = "Hybrid",
                RetrievalSource = "Hybrid"
            };
        }
        foreach (var kr in keywordResults)
        {
            if (merged.TryGetValue(kr.Id, out var existing))
            {
                existing.KeywordScore = kr.Score;
                existing.Content = kr.Content;
            }
            else
            {
                merged[kr.Id] = new SearchResult
                {
                    Id = kr.Id,
                    Content = kr.Content,
                    KeywordScore = kr.Score,
                    VectorScore = 0,
                    SearchType = "Hybrid",
                    RetrievalSource = "Hybrid"
                };
            }
        }

        var results = merged.Values.ToList();
        results = ReciprocalRankFusion(results, options.VectorWeight, options.KeywordWeight);
        results = results.Where(r => r.HybridScore >= options.ScoreThreshold).Take(options.HybridLimit).ToList();

        if (options.RerankingMode != RerankingMode.Disabled)
            results = (await _rerankingService.RerankAsync(results, query, ct)).ToList();

        return results.Take(options.FinalLimit).ToList();
    }

    private static List<SearchResult> ReciprocalRankFusion(List<SearchResult> results, double vectorWeight, double keywordWeight)
    {
        const double k = 60;
        var vectorRanked = results.OrderByDescending(r => r.VectorScore).Select((r, i) => (r, i)).ToList();
        var keywordRanked = results.OrderByDescending(r => r.KeywordScore).Select((r, i) => (r, i)).ToList();

        var scores = new Dictionary<string, double>();
        foreach (var (r, i) in vectorRanked)
            scores[r.Id] = (1.0 / (k + i + 1)) * vectorWeight;
        foreach (var (r, i) in keywordRanked)
        {
            if (scores.ContainsKey(r.Id))
                scores[r.Id] += (1.0 / (k + i + 1)) * keywordWeight;
            else
                scores[r.Id] = (1.0 / (k + i + 1)) * keywordWeight;
        }

        foreach (var r in results)
        {
            r.HybridScore = scores.GetValueOrDefault(r.Id, 0);
        }
        return results.OrderByDescending(r => r.HybridScore).ToList();
    }
}

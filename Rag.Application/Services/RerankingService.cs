using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;

namespace Rag.Application.Services;
public class RerankingService : IRerankingService
{
    private readonly ILogger<RerankingService> _logger;

    public RerankingService(ILogger<RerankingService> logger) => _logger = logger;

    public Task<IReadOnlyList<SearchResult>> RerankAsync(IReadOnlyList<SearchResult> results, string query, CancellationToken ct = default)
    {
        // Score fusion: combine vector and keyword scores
        foreach (var result in results)
        {
            result.HybridScore = (result.VectorScore * 0.5) + (result.KeywordScore * 0.5);
        }
        var reranked = results.OrderByDescending(r => r.HybridScore).ToList();
        return Task.FromResult<IReadOnlyList<SearchResult>>(reranked);
    }
}

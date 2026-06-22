using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface IRerankingService
{
    Task<IReadOnlyList<SearchResult>> RerankAsync(IReadOnlyList<SearchResult> results, string query, CancellationToken ct = default);
}

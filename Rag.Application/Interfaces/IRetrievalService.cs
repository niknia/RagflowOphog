using Rag.Domain.Entities;
using Rag.Domain.ValueObjects;

namespace Rag.Application.Interfaces;
public interface IRetrievalService
{
    Task<IReadOnlyList<SearchResult>> RetrieveAsync(string query, RetrievalOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> VectorSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> KeywordSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> HybridSearchAsync(string query, RetrievalOptions options, CancellationToken ct = default);
}

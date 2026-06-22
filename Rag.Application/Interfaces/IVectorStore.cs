using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface IVectorStore
{
    Task CreateCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default);
    Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default);
    Task InsertAsync(string collectionName, string id, float[] embedding, string metadata, CancellationToken ct = default);
    Task DeleteAsync(string collectionName, string id, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] queryEmbedding, int limit = 10, CancellationToken ct = default);
    Task BatchInsertAsync(string collectionName, IEnumerable<(string Id, float[] Embedding, string Metadata)> items, CancellationToken ct = default);
    Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default);
    Task RebuildCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default);
}

public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public float Score { get; set; }
    public string Metadata { get; set; } = "{}";
}

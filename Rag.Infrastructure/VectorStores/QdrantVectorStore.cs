using System.Text.Json;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Rag.Application.Interfaces;
using Rag.Shared.Helpers;

namespace Rag.Infrastructure.VectorStores;
public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(QdrantClient client, ILogger<QdrantVectorStore> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task CreateCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        await _client.CreateCollectionAsync(collectionName, new VectorParams
        {
            Size = (ulong)vectorDimension,
            Distance = Distance.Cosine
        }, cancellationToken: ct);
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default)
    {
        await _client.DeleteCollectionAsync(collectionName, cancellationToken: ct);
    }

    public async Task InsertAsync(string collectionName, string id, float[] embedding, string metadata, CancellationToken ct = default)
    {
        var pointId = Guid.Parse(id);
        var point = new PointStruct
        {
            Id = pointId,
            Vectors = embedding,
            Payload = { ["metadata"] = metadata }
        };
        await _client.UpsertAsync(collectionName, new[] { point }, cancellationToken: ct);
    }

    public async Task DeleteAsync(string collectionName, string id, CancellationToken ct = default)
    {
        var pointId = Guid.Parse(id);
        await _client.DeleteAsync(collectionName, pointId, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] queryEmbedding, int limit = 10, CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(collectionName, queryEmbedding, limit: (ulong)limit, cancellationToken: ct);
        return results.Select(r => new VectorSearchResult
        {
            Id = r.Id.ToString(),
            Score = (float)r.Score,
            Metadata = r.Payload?.FirstOrDefault(p => p.Key == "metadata").Value?.StringValue ?? "{}"
        }).ToList();
    }

    public async Task BatchInsertAsync(string collectionName, IEnumerable<(string Id, float[] Embedding, string Metadata)> items, CancellationToken ct = default)
    {
        var points = items.Select(i =>
        {
            var pointId = Guid.Parse(i.Id);
            return new PointStruct
            {
                Id = pointId,
                Vectors = i.Embedding,
                Payload = { ["metadata"] = i.Metadata }
            };
        }).ToList();
        await _client.UpsertAsync(collectionName, points, cancellationToken: ct);
    }

    public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync(cancellationToken: ct);
        return collections.Any(c => c == collectionName);
    }

    public async Task RebuildCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        await DeleteCollectionAsync(collectionName, ct);
        await CreateCollectionAsync(collectionName, vectorDimension, ct);
    }
}

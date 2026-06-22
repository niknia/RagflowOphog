using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Shared.Helpers;

namespace Rag.Infrastructure.VectorStores;
public class ChromaVectorStore : IVectorStore
{
    private const string Tenant = "default_tenant";
    private const string Database = "default_database";
    private static string CollectionPath(string collectionName) => $"/api/v2/tenants/{Tenant}/databases/{Database}/collections/{collectionName}";
    private static string CollectionUuidPath(string uuid) => $"/api/v2/tenants/{Tenant}/databases/{Database}/collections/{uuid}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ChromaVectorStore> _logger;
    private readonly ConcurrentDictionary<string, string> _collectionIds = new(StringComparer.OrdinalIgnoreCase);

    public ChromaVectorStore(HttpClient httpClient, ILogger<ChromaVectorStore> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task CreateCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        var checkResponse = await _httpClient.GetAsync(CollectionPath(collectionName), ct);
        if (checkResponse.IsSuccessStatusCode)
        {
            var collection = await checkResponse.Content.ReadFromJsonAsync<ChromaCollectionResponse>(cancellationToken: ct);
            if (collection?.id is not null)
                _collectionIds[collectionName] = collection.id;
            _logger.LogInformation("Chroma collection '{Name}' already exists (UUID: {Uuid})", collectionName, collection?.id);
            return;
        }

        var payload = new
        {
            name = collectionName,
            metadata = new Dictionary<string, object>
            {
                ["hnsw:space"] = "cosine"
            }
        };

        var endpoint = $"/api/v2/tenants/{Tenant}/databases/{Database}/collections";
        var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
        if (response.IsSuccessStatusCode)
        {
            var collection = await response.Content.ReadFromJsonAsync<ChromaCollectionResponse>(cancellationToken: ct);
            if (collection?.id is not null)
                _collectionIds[collectionName] = collection.id;
            _logger.LogInformation("Created Chroma collection '{Name}' (UUID: {Uuid})", collectionName, collection?.id);
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Failed to create collection '{Name}' via {Endpoint}: {StatusCode} - {Error}", collectionName, endpoint, response.StatusCode, errorContent);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetCollectionUuidAsync(string collectionName, CancellationToken ct = default)
    {
        if (_collectionIds.TryGetValue(collectionName, out var uuid))
            return uuid;

        var response = await _httpClient.GetAsync(CollectionPath(collectionName), ct);
        response.EnsureSuccessStatusCode();
        var collection = await response.Content.ReadFromJsonAsync<ChromaCollectionResponse>(cancellationToken: ct);
        uuid = collection?.id ?? throw new InvalidOperationException($"Failed to resolve UUID for collection '{collectionName}'");
        _collectionIds[collectionName] = uuid;
        return uuid;
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default)
    {
        var uuid = await GetCollectionUuidAsync(collectionName, ct);
        var response = await _httpClient.DeleteAsync(CollectionUuidPath(uuid), ct);
        response.EnsureSuccessStatusCode();
        _collectionIds.TryRemove(collectionName, out _);
    }

    public async Task InsertAsync(string collectionName, string id, float[] embedding, string metadata, CancellationToken ct = default)
    {
        var uuid = await GetCollectionUuidAsync(collectionName, ct);
        var payload = new
        {
            ids = new[] { id },
            embeddings = new[] { embedding },
            metadatas = new[] { JsonSerializer.Deserialize<Dictionary<string, object>>(metadata) ?? new() }
        };
        var response = await _httpClient.PostAsJsonAsync($"{CollectionUuidPath(uuid)}/add", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("InsertAsync failed for '{Name}': {StatusCode} - {Error}", collectionName, response.StatusCode, errorContent);
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string collectionName, string id, CancellationToken ct = default)
    {
        var uuid = await GetCollectionUuidAsync(collectionName, ct);
        var payload = new { ids = new[] { id } };
        var request = new HttpRequestMessage(HttpMethod.Post, $"{CollectionUuidPath(uuid)}/delete")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] queryEmbedding, int limit = 10, CancellationToken ct = default)
    {
        var uuid = await GetCollectionUuidAsync(collectionName, ct);
        var payload = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = limit
        };
        var response = await _httpClient.PostAsJsonAsync($"{CollectionUuidPath(uuid)}/query", payload, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>(cancellationToken: ct);
        if (result?.Ids == null || result.Ids.Count == 0)
            return Array.Empty<VectorSearchResult>();

        var searchResults = new List<VectorSearchResult>();
        for (int i = 0; i < result.Ids[0].Count && i < limit; i++)
        {
            searchResults.Add(new VectorSearchResult
            {
                Id = result.Ids[0][i],
                Score = 1.0f - (result.Distances?[0]?[i] ?? 0),
                Metadata = result.Metadatas?[0] != null && i < result.Metadatas[0].Count ? result.Metadatas[0][i].ToString() : "{}"
            });
        }
        return searchResults;
    }

    public async Task BatchInsertAsync(string collectionName, IEnumerable<(string Id, float[] Embedding, string Metadata)> items, CancellationToken ct = default)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;
        var uuid = await GetCollectionUuidAsync(collectionName, ct);
        var payload = new
        {
            ids = itemList.Select(i => i.Id).ToArray(),
            embeddings = itemList.Select(i => i.Embedding).ToArray(),
            metadatas = itemList.Select(i => JsonSerializer.Deserialize<Dictionary<string, object>>(i.Metadata) ?? new()).ToArray()
        };
        var response = await _httpClient.PostAsJsonAsync($"{CollectionUuidPath(uuid)}/add", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("BatchInsertAsync failed for '{Name}': {StatusCode} - {Error}", collectionName, response.StatusCode, errorContent);
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(CollectionPath(collectionName), ct);
        return response.IsSuccessStatusCode;
    }

    public async Task RebuildCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        await DeleteCollectionAsync(collectionName, ct);
        await CreateCollectionAsync(collectionName, vectorDimension, ct);
    }
}

internal class ChromaCollectionResponse
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public string tenant { get; set; } = "";
    public string database { get; set; } = "";
}

internal class ChromaQueryResponse
{
    public List<List<string>> Ids { get; set; } = new();
    public List<List<float>>? Distances { get; set; }
    public List<List<JsonElement>>? Metadatas { get; set; }
}

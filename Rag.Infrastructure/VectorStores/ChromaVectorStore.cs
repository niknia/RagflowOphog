using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Shared.Helpers;

namespace Rag.Infrastructure.VectorStores;
public class ChromaVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChromaVectorStore> _logger;

    public ChromaVectorStore(HttpClient httpClient, ILogger<ChromaVectorStore> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task CreateCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        var payload = new
        {
            name = collectionName,
            metadata = new Dictionary<string, object>
            {
                ["hnsw:space"] = "cosine"
            }
        };

        var endpoints = new[] { "/api/v2/collections", "/api/v1/collections" };
        HttpResponseMessage? response = null;

        foreach (var endpoint in endpoints)
        {
            try
            {
                response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Created Chroma collection '{Name}' via {Endpoint}", collectionName, endpoint);
                    return;
                }

                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Create collection via {Endpoint} failed: {StatusCode} - {Error}", endpoint, response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Create collection via {Endpoint} threw exception", endpoint);
            }
        }

        if (response != null && !response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("All endpoints failed for collection '{Name}'. Last error: {StatusCode} - {Error}", collectionName, response.StatusCode, errorContent);
        }

        response?.EnsureSuccessStatusCode();
        _logger.LogInformation("Created Chroma collection: {Name}", collectionName);
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/collections/{collectionName}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task InsertAsync(string collectionName, string id, float[] embedding, string metadata, CancellationToken ct = default)
    {
        var payload = new
        {
            ids = new[] { id },
            embeddings = new[] { embedding },
            metadatas = new[] { JsonSerializer.Deserialize<Dictionary<string, object>>(metadata) ?? new() }
        };
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{collectionName}/add", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string collectionName, string id, CancellationToken ct = default)
    {
        var payload = new { ids = new[] { id } };
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/collections/{collectionName}/delete")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] queryEmbedding, int limit = 10, CancellationToken ct = default)
    {
        var payload = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = limit
        };
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{collectionName}/query", payload, ct);
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
        var payload = new
        {
            ids = itemList.Select(i => i.Id).ToArray(),
            embeddings = itemList.Select(i => i.Embedding).ToArray(),
            metadatas = itemList.Select(i => JsonSerializer.Deserialize<Dictionary<string, object>>(i.Metadata) ?? new()).ToArray()
        };
        var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{collectionName}/add", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/collections/{collectionName}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task RebuildCollectionAsync(string collectionName, int vectorDimension = 1024, CancellationToken ct = default)
    {
        await DeleteCollectionAsync(collectionName, ct);
        await CreateCollectionAsync(collectionName, vectorDimension, ct);
    }
}

internal class ChromaQueryResponse
{
    public List<List<string>> Ids { get; set; } = new();
    public List<List<float>>? Distances { get; set; }
    public List<List<JsonElement>>? Metadatas { get; set; }
}

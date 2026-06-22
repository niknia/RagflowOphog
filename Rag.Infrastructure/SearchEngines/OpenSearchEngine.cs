using System.Text.Json;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using Rag.Application.Interfaces;

namespace Rag.Infrastructure.SearchEngines;
public class OpenSearchEngine : ISearchEngine
{
    private readonly IElasticClient _client;
    private readonly ILogger<OpenSearchEngine> _logger;

    public OpenSearchEngine(IElasticClient client, ILogger<OpenSearchEngine> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task CreateIndexAsync(string indexName, CancellationToken ct = default)
    {
        if (await IndexExistsAsync(indexName, ct)) return;
        var response = await _client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s.NumberOfShards(1).NumberOfReplicas(0))
            .Map<SearchDocument>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.Content))
                    .Text(t => t.Name(n => n.ContentPersian))
                    .Keyword(k => k.Name(n => n.DocumentId))
                    .Keyword(k => k.Name(n => n.CollectionId))
                )
            ), ct);
    }

    public async Task DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        await _client.Indices.DeleteAsync(indexName, ct: ct);
    }

    public async Task IndexDocumentAsync(string indexName, string id, string text, Dictionary<string, object>? metadata = null, CancellationToken ct = default)
    {
        var doc = new SearchDocument
        {
            Id = id,
            Content = text,
            ContentPersian = text,
            DocumentId = metadata?.GetValueOrDefault("DocumentId")?.ToString() ?? "",
            CollectionId = metadata?.GetValueOrDefault("CollectionId")?.ToString() ?? "default"
        };
        await _client.IndexAsync(doc, i => i.Index(indexName).Id(id), ct);
    }

    public async Task DeleteDocumentAsync(string indexName, string id, CancellationToken ct = default)
    {
        await _client.DeleteAsync<SearchDocument>(id, i => i.Index(indexName), ct);
    }

    public async Task<IReadOnlyList<KeywordSearchResult>> SearchAsync(string indexName, string query, int limit = 10, CancellationToken ct = default)
    {
        if (!await IndexExistsAsync(indexName, ct)) return Array.Empty<KeywordSearchResult>();
        var response = await _client.SearchAsync<SearchDocument>(s => s
            .Index(indexName).Size(limit)
            .Query(q => q.Match(m => m.Field(f => f.Content).Query(query))), ct);
        if (!response.IsValid) return Array.Empty<KeywordSearchResult>();
        return response.Hits.Select(h => new KeywordSearchResult
        {
            Id = h.Id, Score = h.Score ?? 0, Content = h.Source.Content
        }).ToList();
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        var response = await _client.Indices.ExistsAsync(indexName, ct: ct);
        return response.Exists;
    }

    public async Task BulkIndexAsync(string indexName, IEnumerable<(string Id, string Text, Dictionary<string, object> Metadata)> documents, CancellationToken ct = default)
    {
        var bulk = new BulkDescriptor();
        foreach (var (id, text, metadata) in documents)
        {
            var doc = new SearchDocument
            {
                Id = id, Content = text, ContentPersian = text,
                DocumentId = metadata.GetValueOrDefault("DocumentId")?.ToString() ?? "",
                CollectionId = metadata.GetValueOrDefault("CollectionId")?.ToString() ?? "default"
            };
            bulk.Index<SearchDocument>(op => op.Index(indexName).Id(id).Document(doc));
        }
        await _client.BulkAsync(bulk, ct);
    }
}

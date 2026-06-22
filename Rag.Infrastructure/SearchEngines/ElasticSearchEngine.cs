using System.Text.Json;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using Rag.Application.Interfaces;

namespace Rag.Infrastructure.SearchEngines;
public class ElasticSearchEngine : ISearchEngine
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticSearchEngine> _logger;

    public ElasticSearchEngine(IElasticClient client, ILogger<ElasticSearchEngine> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task CreateIndexAsync(string indexName, CancellationToken ct = default)
    {
        if (await IndexExistsAsync(indexName, ct)) return;
        var response = await _client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s.NumberOfShards(1).NumberOfReplicas(0)
                .Analysis(a => a.Analyzers(an => an
                    .Custom("persian_analyzer", ca => ca
                        .Tokenizer("standard")
                        .Filters("lowercase", "stop", "asciifolding"))
                    .Custom("standard_analyzer", ca => ca
                        .Tokenizer("standard")
                        .Filters("lowercase", "stop"))))
            )
            .Map<SearchDocument>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.Content).Analyzer("standard_analyzer"))
                    .Text(t => t.Name(n => n.ContentPersian).Analyzer("persian_analyzer"))
                    .Keyword(k => k.Name(n => n.DocumentId))
                    .Keyword(k => k.Name(n => n.CollectionId))
                    .Number(n => n.Name(f => f.ChunkIndex).Type(NumberType.Integer))
                    .Number(n => n.Name(f => f.PageNumber).Type(NumberType.Integer))
                )
            ), ct);
        if (!response.IsValid)
            _logger.LogError("Failed to create index: {Error}", response.ServerError?.Error?.Reason);
    }

    public async Task DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        var response = await _client.Indices.DeleteAsync(indexName, ct: ct);
        if (!response.IsValid && response.ServerError?.Status != 404)
            _logger.LogError("Failed to delete index: {Error}", response.ServerError?.Error?.Reason);
    }

    public async Task IndexDocumentAsync(string indexName, string id, string text, Dictionary<string, object>? metadata = null, CancellationToken ct = default)
    {
        var doc = new SearchDocument
        {
            Id = id,
            Content = text,
            ContentPersian = text,
            DocumentId = metadata?.GetValueOrDefault("DocumentId")?.ToString() ?? "",
            CollectionId = metadata?.GetValueOrDefault("CollectionId")?.ToString() ?? "default",
            ChunkIndex = int.Parse(metadata?.GetValueOrDefault("ChunkIndex")?.ToString() ?? "0"),
            PageNumber = int.Parse(metadata?.GetValueOrDefault("PageNumber")?.ToString() ?? "0")
        };
        var response = await _client.IndexAsync(doc, i => i.Index(indexName).Id(id), ct);
        if (!response.IsValid)
            _logger.LogError("Failed to index document: {Error}", response.ServerError?.Error?.Reason);
    }

    public async Task DeleteDocumentAsync(string indexName, string id, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync<SearchDocument>(id, i => i.Index(indexName), ct);
        if (!response.IsValid && response.ServerError?.Status != 404)
            _logger.LogError("Failed to delete document: {Error}", response.ServerError?.Error?.Reason);
    }

    public async Task<IReadOnlyList<KeywordSearchResult>> SearchAsync(string indexName, string query, int limit = 10, CancellationToken ct = default)
    {
        if (!await IndexExistsAsync(indexName, ct))
            return Array.Empty<KeywordSearchResult>();

        var response = await _client.SearchAsync<SearchDocument>(s => s
            .Index(indexName)
            .Size(limit)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.Match(m => m.Field(f => f.Content).Query(query).Boost(1.5)),
                        sh => sh.Match(m => m.Field(f => f.ContentPersian).Query(query).Boost(1.2))
                    )
                )
            )
        , ct);

        if (!response.IsValid) return Array.Empty<KeywordSearchResult>();

        return response.Hits.Select(h => new KeywordSearchResult
        {
            Id = h.Id,
            Score = h.Score ?? 0,
            Content = h.Source.Content,
            Metadata = new Dictionary<string, object>
            {
                ["DocumentId"] = h.Source.DocumentId,
                ["CollectionId"] = h.Source.CollectionId,
                ["ChunkIndex"] = h.Source.ChunkIndex,
                ["PageNumber"] = h.Source.PageNumber
            }
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
                Id = id,
                Content = text,
                ContentPersian = text,
                DocumentId = metadata.GetValueOrDefault("DocumentId")?.ToString() ?? "",
                CollectionId = metadata.GetValueOrDefault("CollectionId")?.ToString() ?? "default",
                ChunkIndex = int.Parse(metadata.GetValueOrDefault("ChunkIndex")?.ToString() ?? "0"),
                PageNumber = int.Parse(metadata.GetValueOrDefault("PageNumber")?.ToString() ?? "0")
            };
            bulk.Index<SearchDocument>(op => op.Index(indexName).Id(id).Document(doc));
        }
        var response = await _client.BulkAsync(bulk, ct);
        if (!response.IsValid)
        {
            var reason = response.ServerError?.Error?.Reason;
            var debug = response.DebugInformation;
            _logger.LogError("Bulk index failed: {Error}. Debug: {Debug}", reason, debug);
            throw new InvalidOperationException($"Bulk index failed: {reason ?? "unknown"}");
        }
    }
}

public class SearchDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentPersian { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
}

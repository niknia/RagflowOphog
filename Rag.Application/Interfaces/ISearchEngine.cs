namespace Rag.Application.Interfaces;
public interface ISearchEngine
{
    Task CreateIndexAsync(string indexName, CancellationToken ct = default);
    Task DeleteIndexAsync(string indexName, CancellationToken ct = default);
    Task IndexDocumentAsync(string indexName, string id, string text, Dictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task DeleteDocumentAsync(string indexName, string id, CancellationToken ct = default);
    Task<IReadOnlyList<KeywordSearchResult>> SearchAsync(string indexName, string query, int limit = 10, CancellationToken ct = default);
    Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default);
    Task BulkIndexAsync(string indexName, IEnumerable<(string Id, string Text, Dictionary<string, object> Metadata)> documents, CancellationToken ct = default);
}

public class KeywordSearchResult
{
    public string Id { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

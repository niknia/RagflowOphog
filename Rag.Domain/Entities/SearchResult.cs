namespace Rag.Domain.Entities;
public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public double VectorScore { get; set; }
    public double KeywordScore { get; set; }
    public double HybridScore { get; set; }
    public string SearchType { get; set; } = "Hybrid";
    public string RetrievalSource { get; set; } = string.Empty;
}

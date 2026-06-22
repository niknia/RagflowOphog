using Rag.Domain.Enums;

namespace Rag.Contracts.DTOs;
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public RetrievalMode Mode { get; set; } = RetrievalMode.Hybrid;
    public string CollectionId { get; set; } = "default";
    public int Limit { get; set; } = 10;
    public double ScoreThreshold { get; set; }
    public RerankingMode RerankingMode { get; set; } = RerankingMode.ScoreFusion;
}

public class SearchResponse
{
    public string Query { get; set; } = string.Empty;
    public RetrievalMode Mode { get; set; }
    public List<SearchResultDto> Results { get; set; } = new();
    public long LatencyMs { get; set; }
}

public class SearchResultDto
{
    public string DocumentName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public double Score { get; set; }
    public string SearchType { get; set; } = string.Empty;
}

using MediatR;
using Rag.Contracts.DTOs;
using Rag.Domain.Enums;

namespace Rag.Contracts.Queries;
public class SearchQuery : IRequest<SearchResponse>
{
    public string Query { get; set; } = string.Empty;
    public RetrievalMode Mode { get; set; } = RetrievalMode.Hybrid;
    public string CollectionId { get; set; } = "default";
    public int Limit { get; set; } = 10;
    public double ScoreThreshold { get; set; }
    public RerankingMode RerankingMode { get; set; } = RerankingMode.ScoreFusion;
}

using MediatR;
using Rag.Application.Interfaces;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.ValueObjects;

namespace Rag.Application.Handlers;
public class SearchHandler : IRequestHandler<SearchQuery, SearchResponse>
{
    private readonly IRetrievalService _retrievalService;
    public SearchHandler(IRetrievalService retrievalService) => _retrievalService = retrievalService;
    public async Task<SearchResponse> Handle(SearchQuery request, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var options = new RetrievalOptions
        {
            Mode = request.Mode, CollectionId = request.CollectionId,
            FinalLimit = request.Limit, ScoreThreshold = request.ScoreThreshold
        };
        var results = await _retrievalService.RetrieveAsync(request.Query, options, ct);
        sw.Stop();
        return new SearchResponse
        {
            Query = request.Query, Mode = request.Mode,
            Results = results.Select(r => new SearchResultDto
            {
                DocumentName = r.DocumentName, CollectionName = r.CollectionName,
                ChunkIndex = r.ChunkIndex, PageNumber = r.PageNumber,
                Content = r.Content, Section = r.Section,
                Score = r.HybridScore > 0 ? r.HybridScore : Math.Max(r.VectorScore, r.KeywordScore),
                SearchType = r.SearchType
            }).ToList(),
            LatencyMs = sw.ElapsedMilliseconds
        };
    }
}

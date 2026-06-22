using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.Enums;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/search")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator) => _mediator = mediator;

    /// <summary>Perform a search with the specified retrieval mode (default: Hybrid).</summary>
    /// <param name="request">Search parameters including query text, mode, collection, and filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Search results with scores and metadata.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromBody] SearchRequest request,
        CancellationToken ct = default)
    {
        var query = new SearchQuery
        {
            Query = request.Query,
            Mode = request.Mode,
            CollectionId = request.CollectionId,
            Limit = request.Limit,
            ScoreThreshold = request.ScoreThreshold,
            RerankingMode = request.RerankingMode
        };
        return Ok(await _mediator.Send(query, ct));
    }

    /// <summary>Search using hybrid retrieval (vector + keyword with RRF fusion).</summary>
    /// <param name="request">Search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Hybrid search results.</response>
    [HttpPost("hybrid")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> HybridSearch(
        [FromBody] SearchRequest request,
        CancellationToken ct = default)
    {
        request.Mode = RetrievalMode.Hybrid;
        var query = new SearchQuery
        {
            Query = request.Query, Mode = RetrievalMode.Hybrid,
            CollectionId = request.CollectionId, Limit = request.Limit
        };
        return Ok(await _mediator.Send(query, ct));
    }

    /// <summary>Search using vector-only retrieval (embedding similarity).</summary>
    /// <param name="request">Search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Vector search results.</response>
    [HttpPost("vector")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> VectorSearch(
        [FromBody] SearchRequest request,
        CancellationToken ct = default)
    {
        request.Mode = RetrievalMode.VectorOnly;
        var query = new SearchQuery { Query = request.Query, Mode = RetrievalMode.VectorOnly, CollectionId = request.CollectionId, Limit = request.Limit };
        return Ok(await _mediator.Send(query, ct));
    }

    /// <summary>Search using keyword-only retrieval (Elasticsearch/OpenSearch full-text).</summary>
    /// <param name="request">Search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Keyword search results.</response>
    [HttpPost("keyword")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> KeywordSearch(
        [FromBody] SearchRequest request,
        CancellationToken ct = default)
    {
        request.Mode = RetrievalMode.KeywordOnly;
        var query = new SearchQuery { Query = request.Query, Mode = RetrievalMode.KeywordOnly, CollectionId = request.CollectionId, Limit = request.Limit };
        return Ok(await _mediator.Send(query, ct));
    }
}

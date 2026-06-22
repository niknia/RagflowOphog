using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/collections")]
[Produces("application/json")]
public class CollectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollectionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new document collection with vector dimension configuration.</summary>
    /// <param name="command">Collection name, description, and vector dimension.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Collection created successfully.</response>
    /// <response code="400">Invalid collection data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionDto>> Create(
        [FromBody] CreateCollectionCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>List all available collections.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of collections.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CollectionDto>>> GetAll(CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new GetCollectionsQuery(), ct));
    }

    /// <summary>Delete a collection and its associated documents and chunks.</summary>
    /// <param name="id">Collection identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Collection deleted successfully.</response>
    /// <response code="404">Collection not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(string id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteCollectionCommand { Id = id }, ct);
        return NoContent();
    }
}

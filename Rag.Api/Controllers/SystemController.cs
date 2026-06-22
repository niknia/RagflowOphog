using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/system")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get system status including connected services and document counts.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">System status information.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatusResponse>> GetStatus(CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new GetSystemStatusQuery(), ct));
    }

    /// <summary>List configured providers (vector stores, search engines, models).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Provider configuration details.</response>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ProvidersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvidersResponse>> GetProviders(CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new GetProvidersQuery(), ct));
    }
}

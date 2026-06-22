using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new chat session backed by Semantic Kernel.</summary>
    /// <param name="command">Chat creation parameters (title, model, collection).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Chat session created.</response>
    [HttpPost("create")]
    [ProducesResponseType(typeof(ChatCreateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatCreateResponse>> Create(
        [FromBody] CreateChatCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Send a message in a chat session and receive an AI-generated response with citations.</summary>
    /// <param name="id">Chat session GUID.</param>
    /// <param name="request">Message content and retrieval options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">AI response with retrieved context citations.</response>
    /// <response code="404">Chat session not found.</response>
    [HttpPost("{id:guid}/message")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(
        Guid id,
        [FromBody] ChatMessageRequest request,
        CancellationToken ct = default)
    {
        var command = new SendMessageCommand
        {
            ChatId = id,
            Message = request.Message,
            RetrievalMode = request.RetrievalMode,
            CollectionId = request.CollectionId
        };
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Retrieve the full message history of a chat session.</summary>
    /// <param name="id">Chat session GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Chat session with message history and citations.</response>
    /// <response code="404">Chat session not found.</response>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(ChatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatDto>> GetHistory(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetChatHistoryQuery { Id = id }, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }
}

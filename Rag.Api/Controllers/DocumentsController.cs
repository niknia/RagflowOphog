using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rag.Application.Interfaces;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.Enums;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProcessingProgressRepository _progressRepo;

    public DocumentsController(IMediator mediator, IProcessingProgressRepository progressRepo)
    {
        _mediator = mediator;
        _progressRepo = progressRepo;
    }

    /// <summary>Upload a document for processing and indexing.</summary>
    /// <param name="file">The document file (TXT, CSV, JSON, XML, HTML). Max size: 50 MB.</param>
    /// <param name="collectionId">Target collection identifier.</param>
    /// <param name="collectionName">Target collection display name.</param>
    /// <param name="chunkingStrategy">Chunking strategy: Section, Paragraph, Page, Sentence, or Hybrid.</param>
    /// <param name="maxChunkSize">Maximum chunk size in tokens.</param>
    /// <param name="chunkOverlap">Overlap between consecutive chunks.</param>
    /// <param name="embeddingDimensions">Embedding vector dimensions.</param>
    /// <param name="normalizeEmbeddings">Whether to normalize embedding vectors.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Document uploaded and queued for processing.</response>
    /// <response code="400">No file provided or file is empty.</response>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(
        IFormFile file,
        [FromQuery] string collectionId = "default",
        [FromQuery] string collectionName = "knowledge-base",
        [FromQuery] string chunkingStrategy = "Hybrid",
        [FromQuery] int maxChunkSize = 1024,
        [FromQuery] int chunkOverlap = 128,
        [FromQuery] int embeddingDimensions = 1024,
        [FromQuery] bool normalizeEmbeddings = true,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");
        using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            CollectionId = collectionId,
            CollectionName = collectionName,
            ChunkingStrategy = chunkingStrategy,
            MaxChunkSize = maxChunkSize,
            ChunkOverlap = chunkOverlap,
            EmbeddingDimensions = embeddingDimensions,
            NormalizeEmbeddings = normalizeEmbeddings
        };
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>List all documents with optional filtering and pagination.</summary>
    /// <param name="page">Page number (1-indexed).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="status">Filter by document status (Pending, Processing, Indexed, Failed).</param>
    /// <param name="collectionId">Filter by collection identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Paginated list of documents.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DocumentListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? collectionId = null,
        CancellationToken ct = default)
    {
        var query = new GetDocumentsQuery
        {
            Page = page, PageSize = pageSize, Status = status, CollectionId = collectionId
        };
        return Ok(await _mediator.Send(query, ct));
    }

    /// <summary>Get a document by its unique identifier.</summary>
    /// <param name="id">Document GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Document details.</response>
    /// <response code="404">Document not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDocumentQuery { Id = id }, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Delete a document and its associated chunks.</summary>
    /// <param name="id">Document GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Document deleted successfully.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteDocumentCommand { Id = id }, ct);
        return NoContent();
    }

    /// <summary>Queue a document for reindexing.</summary>
    /// <param name="id">Document GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Document queued for reindexing.</response>
    [HttpPost("reindex/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Reindex(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new ReindexDocumentCommand { Id = id }, ct);
        return Ok(new { message = "Document queued for reindexing" });
    }

    /// <summary>Get processing progress for a specific document.</summary>
    /// <param name="id">Document GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Processing progress details.</response>
    /// <response code="404">No progress record found.</response>
    [HttpGet("{id:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetProgress(Guid id, CancellationToken ct = default)
    {
        var progress = await _progressRepo.GetByDocumentIdAsync(id, ct);
        if (progress == null) return NotFound(new { error = "No progress record found" });
        return Ok(new
        {
            documentId = progress.DocumentId,
            fileName = progress.FileName,
            percent = progress.Percent,
            stage = progress.Stage,
            message = progress.Message,
            status = progress.Status,
            createdAt = progress.CreatedAt,
            updatedAt = progress.UpdatedAt,
            completedAt = progress.CompletedAt
        });
    }

    /// <summary>List all processed (completed) documents.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of completed processing records.</response>
    [HttpGet("processed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetProcessed(CancellationToken ct = default)
    {
        var completed = await _progressRepo.GetCompletedAsync(ct);
        return Ok(completed.Select(p => new
        {
            documentId = p.DocumentId,
            fileName = p.FileName,
            percent = p.Percent,
            stage = p.Stage,
            status = p.Status,
            completedAt = p.CompletedAt
        }).ToList());
    }

    /// <summary>List currently processing documents with progress.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of in-progress processing records.</response>
    [HttpGet("processing-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetProcessingStatus(CancellationToken ct = default)
    {
        var inProgress = await _progressRepo.GetInProgressAsync(ct);
        return Ok(inProgress.Select(p => new
        {
            documentId = p.DocumentId,
            fileName = p.FileName,
            percent = p.Percent,
            stage = p.Stage,
            message = p.Message,
            status = p.Status,
            updatedAt = p.UpdatedAt
        }).ToList());
    }
}

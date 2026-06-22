using Microsoft.AspNetCore.Mvc;
using Rag.Application.Interfaces;

namespace Rag.Api.Controllers;

[ApiController]
[Route("api/vectorstore")]
[Produces("application/json")]
public class VectorStoreController : ControllerBase
{
    private readonly IVectorStore _vectorStore;

    public VectorStoreController(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus([FromQuery] string collectionName = "knowledge-base", CancellationToken ct = default)
    {
        var exists = await _vectorStore.CollectionExistsAsync(collectionName, ct);
        return Ok(new
        {
            collectionName,
            exists,
            status = exists ? "available" : "not_found"
        });
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search(
        [FromQuery] string query = "",
        [FromQuery] string collectionName = "knowledge-base",
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Query parameter is required" });

        if (!await _vectorStore.CollectionExistsAsync(collectionName, ct))
            return NotFound(new { error = $"Collection '{collectionName}' not found" });

        return Ok(new
        {
            collectionName,
            message = "Use the /api/chat/query endpoint with retrievalMode=vector for full vector search"
        });
    }
}

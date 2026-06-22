using Rag.Domain.ValueObjects;

namespace Rag.Infrastructure.Chunking;
public interface IChunkingService
{
    Task<IReadOnlyList<ChunkResult>> ChunkAsync(string text, ChunkingOptions options, CancellationToken ct = default);
}

public class ChunkResult
{
    public string Content { get; set; } = string.Empty;
    public int Index { get; set; }
    public int TokenCount { get; set; }
    public int PageNumber { get; set; }
    public string Section { get; set; } = string.Empty;
}

using Rag.Domain.Enums;

namespace Rag.Domain.Entities;
public class Chunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string CollectionId { get; set; } = "default";
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public int PageNumber { get; set; }
    public string Section { get; set; } = string.Empty;
    public Language Language { get; set; } = Language.Unknown;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string VectorId { get; set; } = string.Empty;
    public string SearchDocumentId { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public bool IsIndexed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

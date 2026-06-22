namespace Rag.Domain.Entities;
public class Collection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VectorDimension { get; set; } = 1024;
    public string DistanceMetric { get; set; } = "Cosine";
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

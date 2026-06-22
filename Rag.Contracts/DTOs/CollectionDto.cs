namespace Rag.Contracts.DTOs;
public class CollectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VectorDimension { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VectorDimension { get; set; } = 1024;
}

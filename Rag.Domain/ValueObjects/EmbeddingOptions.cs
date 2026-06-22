namespace Rag.Domain.ValueObjects;
public class EmbeddingOptions
{
    public string Model { get; set; } = "bge-m3";
    public int Dimensions { get; set; } = 1024;
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public int BatchSize { get; set; } = 16;
    public bool NormalizeEmbeddings { get; set; } = true;
}

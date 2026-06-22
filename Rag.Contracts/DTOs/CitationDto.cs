namespace Rag.Contracts.DTOs;
public class CitationDto
{
    public string DocumentName { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int ChunkNumber { get; set; }
    public string RetrievalSource { get; set; } = string.Empty;
    public double VectorScore { get; set; }
    public double ElasticScore { get; set; }
    public double Score { get; set; }
    public string SearchType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

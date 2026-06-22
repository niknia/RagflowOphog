using Rag.Domain.Enums;

namespace Rag.Contracts.DTOs;
public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentStatus Status { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public Language DetectedLanguage { get; set; }
    public int ChunkCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class DocumentUploadResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string Message { get; set; } = string.Empty;
}

public class DocumentListResponse
{
    public List<DocumentDto> Documents { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

using Rag.Domain.Enums;

namespace Rag.Domain.Entities;
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DocumentFileType FileType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string CollectionId { get; set; } = "default";
    public string CollectionName { get; set; } = "knowledge-base";
    public Language DetectedLanguage { get; set; } = Language.Unknown;
    public string OriginalContent { get; set; } = string.Empty;
    public string CleanedContent { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

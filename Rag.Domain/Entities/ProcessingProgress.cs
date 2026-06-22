namespace Rag.Domain.Entities;
public class ProcessingProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string Stage { get; set; } = "Queued";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

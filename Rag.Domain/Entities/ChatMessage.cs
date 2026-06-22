using Rag.Domain.Enums;

namespace Rag.Domain.Entities;
public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public RetrievalMode RetrievalMode { get; set; } = RetrievalMode.Hybrid;
    public string Citations { get; set; } = "[]";
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public long LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

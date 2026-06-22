namespace Rag.Domain.Entities;
public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CollectionId { get; set; } = "default";
    public string Model { get; set; } = "qwen3";
    public string SessionId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

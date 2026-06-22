using Rag.Domain.Enums;

namespace Rag.Contracts.DTOs;
public class ChatDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public RetrievalMode RetrievalMode { get; set; }
    public List<CitationDto> Citations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ChatCreateResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public RetrievalMode RetrievalMode { get; set; } = RetrievalMode.Hybrid;
    public string CollectionId { get; set; } = "default";
}

public class ChatMessageResponse
{
    public string Content { get; set; } = string.Empty;
    public List<CitationDto> Citations { get; set; } = new();
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public long LatencyMs { get; set; }
}

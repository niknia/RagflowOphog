using MediatR;
using Rag.Contracts.DTOs;
using Rag.Domain.Enums;

namespace Rag.Contracts.Commands;
public class CreateChatCommand : IRequest<ChatCreateResponse>
{
    public string Title { get; set; } = string.Empty;
    public string CollectionId { get; set; } = "default";
    public string Model { get; set; } = "qwen3";
}

public class SendMessageCommand : IRequest<ChatMessageResponse>
{
    public Guid ChatId { get; set; }
    public string Message { get; set; } = string.Empty;
    public RetrievalMode RetrievalMode { get; set; } = RetrievalMode.Hybrid;
    public string CollectionId { get; set; } = "default";
}

public class StreamMessageCommand : IRequest<IAsyncEnumerable<string>>
{
    public Guid ChatId { get; set; }
    public string Message { get; set; } = string.Empty;
    public RetrievalMode RetrievalMode { get; set; } = RetrievalMode.Hybrid;
    public string CollectionId { get; set; } = "default";
}

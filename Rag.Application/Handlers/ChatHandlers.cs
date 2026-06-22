using MediatR;
using Microsoft.Extensions.Logging;
using Rag.Application.Services;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.Entities;
using Rag.Application.Interfaces;

namespace Rag.Application.Handlers;
public class CreateChatHandler : IRequestHandler<CreateChatCommand, ChatCreateResponse>
{
    private readonly IChatRepository _repository;
    public CreateChatHandler(IChatRepository repository) => _repository = repository;
    public async Task<ChatCreateResponse> Handle(CreateChatCommand request, CancellationToken ct)
    {
        var chat = new Chat
        {
            Title = request.Title,
            CollectionId = request.CollectionId,
            Model = request.Model,
            SessionId = Guid.NewGuid().ToString()
        };
        chat = await _repository.CreateAsync(chat, ct);
        return new ChatCreateResponse { Id = chat.Id, Title = chat.Title };
    }
}

public class SendMessageHandler : IRequestHandler<SendMessageCommand, ChatMessageResponse>
{
    private readonly ChatService _chatService;
    private readonly ILogger<SendMessageHandler> _logger;
    public SendMessageHandler(ChatService chatService, ILogger<SendMessageHandler> logger)
    {
        _chatService = chatService; _logger = logger;
    }
    public async Task<ChatMessageResponse> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var (response, citations) = await _chatService.SendMessageAsync(
            request.ChatId, request.Message, request.RetrievalMode, request.CollectionId, ct);
        sw.Stop();
        return new ChatMessageResponse
        {
            Content = response,
            Citations = citations.Select(c => new CitationDto
            {
                DocumentName = c.DocumentName, Collection = c.Collection,
                PageNumber = c.PageNumber, ChunkNumber = c.ChunkNumber,
                RetrievalSource = c.RetrievalSource, Score = c.Score,
                SearchType = c.SearchType, Content = c.Content
            }).ToList(),
            LatencyMs = sw.ElapsedMilliseconds
        };
    }
}

public class GetChatHistoryHandler : IRequestHandler<GetChatHistoryQuery, ChatDto?>
{
    private readonly IChatRepository _repository;
    public GetChatHistoryHandler(IChatRepository repository) => _repository = repository;
    public async Task<ChatDto?> Handle(GetChatHistoryQuery request, CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(request.Id, ct);
        if (chat == null) return null;
        return new ChatDto
        {
            Id = chat.Id, Title = chat.Title, Model = chat.Model,
            CollectionId = chat.CollectionId, CreatedAt = chat.CreatedAt,
            Messages = chat.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id, Role = m.Role, Content = m.Content,
                RetrievalMode = m.RetrievalMode, CreatedAt = m.CreatedAt
            }).ToList()
        };
    }
}

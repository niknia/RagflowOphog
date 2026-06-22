using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Chat> CreateAsync(Chat chat, CancellationToken ct = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid chatId, CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;

namespace Rag.Infrastructure.Persistence;
public class ChatRepository : IChatRepository
{
    private readonly RagDbContext _context;
    public ChatRepository(RagDbContext context) => _context = context;

    public async Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Chats.Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Chat> CreateAsync(Chat chat, CancellationToken ct = default)
    {
        _context.Chats.Add(chat);
        await _context.SaveChangesAsync(ct);
        return chat;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
        return message;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid chatId, CancellationToken ct = default)
        => await _context.ChatMessages.Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt).ToListAsync(ct);
}

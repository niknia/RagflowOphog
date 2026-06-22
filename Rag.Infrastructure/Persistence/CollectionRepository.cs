using Microsoft.EntityFrameworkCore;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;

namespace Rag.Infrastructure.Persistence;
public class CollectionRepository : ICollectionRepository
{
    private readonly RagDbContext _context;
    public CollectionRepository(RagDbContext context) => _context = context;

    public async Task<Collection?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.Collections.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Collection>> GetAllAsync(CancellationToken ct = default)
        => await _context.Collections.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<Collection> CreateAsync(Collection collection, CancellationToken ct = default)
    {
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync(ct);
        return collection;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var collection = await _context.Collections.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (collection != null)
        {
            _context.Collections.Remove(collection);
            await _context.SaveChangesAsync(ct);
        }
    }
}

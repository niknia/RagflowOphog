using Microsoft.EntityFrameworkCore;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;
using Rag.Domain.Enums;

namespace Rag.Infrastructure.Persistence;
public class DocumentRepository : IDocumentRepository
{
    private readonly RagDbContext _context;

    public DocumentRepository(RagDbContext context) => _context = context;

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Document>> GetAllAsync(int page = 1, int pageSize = 20, string? status = null, string? collectionId = null, CancellationToken ct = default)
    {
        var query = _context.Documents.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status.ToString() == status);
        if (!string.IsNullOrEmpty(collectionId))
            query = query.Where(d => d.CollectionId == collectionId);
        return await query.OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> CountAsync(string? status = null, string? collectionId = null, CancellationToken ct = default)
    {
        var query = _context.Documents.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status.ToString() == status);
        if (!string.IsNullOrEmpty(collectionId))
            query = query.Where(d => d.CollectionId == collectionId);
        return await query.CountAsync(ct);
    }

    public async Task<Document> AddAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(ct);
        return document;
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        _context.Entry(document).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc != null)
        {
            doc.IsDeleted = true;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Document>> GetPendingDocumentsAsync(int limit = 10, CancellationToken ct = default)
        => await _context.Documents
            .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Failed)
            .OrderBy(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}

using Microsoft.EntityFrameworkCore;
using Rag.Application.Interfaces;
using Rag.Domain.Entities;

namespace Rag.Infrastructure.Persistence;
public class ProcessingProgressRepository : IProcessingProgressRepository
{
    private readonly RagDbContext _context;

    public ProcessingProgressRepository(RagDbContext context) => _context = context;

    public async Task<ProcessingProgress?> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default)
        => await _context.ProcessingProgresses.FirstOrDefaultAsync(p => p.DocumentId == documentId, ct);

    public async Task<List<ProcessingProgress>> GetAllAsync(CancellationToken ct = default)
        => await _context.ProcessingProgresses.OrderByDescending(p => p.UpdatedAt).ToListAsync(ct);

    public async Task<List<ProcessingProgress>> GetCompletedAsync(CancellationToken ct = default)
        => await _context.ProcessingProgresses.Where(p => p.Status == "Completed").OrderByDescending(p => p.UpdatedAt).ToListAsync(ct);

    public async Task<List<ProcessingProgress>> GetInProgressAsync(CancellationToken ct = default)
        => await _context.ProcessingProgresses.Where(p => p.Status == "Processing" || p.Status == "Pending").OrderByDescending(p => p.UpdatedAt).ToListAsync(ct);

    public async Task<ProcessingProgress> AddAsync(ProcessingProgress progress, CancellationToken ct = default)
    {
        _context.ProcessingProgresses.Add(progress);
        await _context.SaveChangesAsync(ct);
        return progress;
    }

    public async Task UpdateAsync(ProcessingProgress progress, CancellationToken ct = default)
    {
        _context.Entry(progress).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }
}

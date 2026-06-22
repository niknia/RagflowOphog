using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface IProcessingProgressRepository
{
    Task<ProcessingProgress?> GetByDocumentIdAsync(Guid documentId, CancellationToken ct = default);
    Task<List<ProcessingProgress>> GetAllAsync(CancellationToken ct = default);
    Task<List<ProcessingProgress>> GetCompletedAsync(CancellationToken ct = default);
    Task<List<ProcessingProgress>> GetInProgressAsync(CancellationToken ct = default);
    Task<ProcessingProgress> AddAsync(ProcessingProgress progress, CancellationToken ct = default);
    Task UpdateAsync(ProcessingProgress progress, CancellationToken ct = default);
}

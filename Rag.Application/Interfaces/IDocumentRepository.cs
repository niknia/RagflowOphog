using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetAllAsync(int page = 1, int pageSize = 20, string? status = null, string? collectionId = null, CancellationToken ct = default);
    Task<int> CountAsync(string? status = null, string? collectionId = null, CancellationToken ct = default);
    Task<Document> AddAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetPendingDocumentsAsync(int limit = 10, CancellationToken ct = default);
}

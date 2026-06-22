using Rag.Domain.Entities;

namespace Rag.Application.Interfaces;
public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Collection>> GetAllAsync(CancellationToken ct = default);
    Task<Collection> CreateAsync(Collection collection, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

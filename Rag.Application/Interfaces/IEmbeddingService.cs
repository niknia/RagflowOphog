namespace Rag.Application.Interfaces;
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}

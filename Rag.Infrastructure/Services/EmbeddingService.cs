using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Infrastructure.Configuration;
using Rag.Shared.Extensions;

namespace Rag.Infrastructure.Services;
public class EmbeddingService : IEmbeddingService
{
    private readonly ILLMProvider _provider;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly EmbeddingConfiguration _config;

    public EmbeddingService(ILLMProvider provider, EmbeddingConfiguration config, ILogger<EmbeddingService> logger)
    {
        _provider = provider;
        _logger = logger;
        _config = config;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var embedding = await _provider.GenerateEmbeddingAsync(text, ct);
            return _config.NormalizeEmbeddings ? embedding.Normalize() : embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding via {Provider}", _provider.Name);
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var tasks = texts.Select(t => GenerateEmbeddingAsync(t, ct));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        return await _provider.HealthCheckAsync(ct);
    }
}

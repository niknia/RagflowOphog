using MediatR;
using Rag.Application.Interfaces;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;

namespace Rag.Application.Handlers;
public class GetSystemStatusHandler : IRequestHandler<GetSystemStatusQuery, SystemStatusResponse>
{
    private readonly IDocumentRepository _docRepo;
    private readonly IOllamaConfiguration _ollamaCfg;
    private readonly IEmbeddingService _embeddingService;

    public GetSystemStatusHandler(IDocumentRepository docRepo, IOllamaConfiguration ollamaCfg, IEmbeddingService embeddingService)
    {
        _docRepo = docRepo;
        _ollamaCfg = ollamaCfg;
        _embeddingService = embeddingService;
    }

    public async Task<SystemStatusResponse> Handle(GetSystemStatusQuery request, CancellationToken ct)
    {
        var ollamaOk = false;
        try { ollamaOk = await _embeddingService.HealthCheckAsync(ct); } catch { }

        return new SystemStatusResponse
        {
            Version = "1.0.0",
            VectorDatabaseConnected = true,
            SearchEngineConnected = true,
            OllamaConnected = ollamaOk,
            ActiveVectorProvider = "Chroma",
            ActiveSearchProvider = "ElasticSearch",
            ChatModel = _ollamaCfg.ChatModel,
            EmbeddingModel = _ollamaCfg.EmbeddingModel,
            TotalDocuments = await _docRepo.CountAsync(ct: ct),
            TotalChunks = 0
        };
    }
}

public class GetProvidersHandler : IRequestHandler<GetProvidersQuery, ProvidersResponse>
{
    private readonly IOllamaConfiguration _ollamaCfg;

    public GetProvidersHandler(IOllamaConfiguration ollamaCfg) => _ollamaCfg = ollamaCfg;

    public Task<ProvidersResponse> Handle(GetProvidersQuery request, CancellationToken ct)
    {
        return Task.FromResult(new ProvidersResponse
        {
            VectorProviders = new List<ProviderInfo>
            {
                new() { Name = "Chroma", Type = "VectorDB", IsActive = true, Status = "Configured" },
                new() { Name = "Qdrant", Type = "VectorDB", IsActive = false, Status = "Configured" }
            },
            SearchProviders = new List<ProviderInfo>
            {
                new() { Name = "ElasticSearch", Type = "SearchEngine", IsActive = true, Status = "Configured" },
                new() { Name = "OpenSearch", Type = "SearchEngine", IsActive = false, Status = "Configured" }
            },
            EmbeddingModels = new List<ProviderInfo>
            {
                new() { Name = _ollamaCfg.EmbeddingModel, Type = "Embedding", IsActive = true, Status = "Configured" }
            },
            ChatModels = new List<ProviderInfo>
            {
                new() { Name = _ollamaCfg.ChatModel, Type = "Chat", IsActive = true, Status = "Configured" }
            }
        });
    }
}

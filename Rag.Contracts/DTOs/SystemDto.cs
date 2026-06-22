namespace Rag.Contracts.DTOs;
public class SystemStatusResponse
{
    public string Version { get; set; } = string.Empty;
    public bool VectorDatabaseConnected { get; set; }
    public bool SearchEngineConnected { get; set; }
    public bool OllamaConnected { get; set; }
    public string ActiveVectorProvider { get; set; } = string.Empty;
    public string ActiveSearchProvider { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
}

public class ProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProvidersResponse
{
    public List<ProviderInfo> VectorProviders { get; set; } = new();
    public List<ProviderInfo> SearchProviders { get; set; } = new();
    public List<ProviderInfo> EmbeddingModels { get; set; } = new();
    public List<ProviderInfo> ChatModels { get; set; } = new();
}

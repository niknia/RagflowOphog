using Rag.Application.Interfaces;

namespace Rag.Infrastructure.Configuration;
public class VectorStoreConfiguration
{
    public string Provider { get; set; } = "Chroma";
    public string DefaultCollection { get; set; } = "knowledge-base";
}

public class ChromaConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
}

public class QdrantConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
}

public class SearchConfiguration
{
    public string Provider { get; set; } = "ElasticSearch";
}

public class ElasticSearchConfiguration
{
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "knowledge-index";
}

public class OllamaConfiguration : IOllamaConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen3";
    public string EmbeddingModel { get; set; } = "bge-m3";
}

public class EmbeddingConfiguration
{
    public string Model { get; set; } = "bge-m3";
    public int Dimensions { get; set; } = 1024;
    public int BatchSize { get; set; } = 16;
    public bool NormalizeEmbeddings { get; set; } = true;
}

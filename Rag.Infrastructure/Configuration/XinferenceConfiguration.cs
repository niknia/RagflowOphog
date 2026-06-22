namespace Rag.Infrastructure.Configuration;
public class XinferenceConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:9997";
    public string ModelUid { get; set; } = "bge-m3";
    public string ChatModelUid { get; set; } = "qwen2.5-instruct";
    public string EmbeddingModelUid { get; set; } = "bge-m3";
    public string ApiKey { get; set; } = "EMPTY";
    public int Dimensions { get; set; } = 1024;
    public bool NormalizeEmbeddings { get; set; } = true;
}

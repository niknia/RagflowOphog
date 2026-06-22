namespace Rag.Application.Interfaces;
public interface IOllamaConfiguration
{
    string BaseUrl { get; }
    string ChatModel { get; }
    string EmbeddingModel { get; }
}

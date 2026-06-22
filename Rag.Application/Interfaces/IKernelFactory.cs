using Microsoft.SemanticKernel;

namespace Rag.Application.Interfaces;
public interface IKernelFactory
{
    Kernel CreateChatKernel(string model = "qwen3");
    Kernel CreateEmbeddingKernel(string model = "bge-m3");
}

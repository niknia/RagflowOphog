using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Rag.Application.Interfaces;

namespace Rag.Application.SemanticKernel.Factories;
public class KernelFactory : IKernelFactory
{
    private readonly ILogger<KernelFactory> _logger;
    private readonly IOllamaConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _appConfig;

    public KernelFactory(
        IOllamaConfiguration config,
        IServiceProvider serviceProvider,
        IConfiguration appConfig,
        ILogger<KernelFactory> logger)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _appConfig = appConfig;
        _logger = logger;
    }

    public Kernel CreateChatKernel(string model = "qwen3")
    {
        var provider = _appConfig["LLMProvider:Provider"] ?? "Ollama";

        if (provider.Equals("Xinference", StringComparison.OrdinalIgnoreCase))
        {
            var chatService = _serviceProvider.GetService<IChatCompletionService>();
            if (chatService != null)
            {
                var builder = Kernel.CreateBuilder();
                builder.Services.AddSingleton(chatService);
                return builder.Build();
            }
        }

        var builder2 = Kernel.CreateBuilder();
        builder2.AddOllamaChatCompletion(model ?? _config.ChatModel, new Uri(_config.BaseUrl));
        return builder2.Build();
    }

    public Kernel CreateEmbeddingKernel(string model = "bge-m3")
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaEmbeddingGenerator(endpoint: new Uri(_config.BaseUrl), modelId: model ?? _config.EmbeddingModel);
        return builder.Build();
    }
}

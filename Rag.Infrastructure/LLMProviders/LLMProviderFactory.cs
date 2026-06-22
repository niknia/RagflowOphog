using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Infrastructure.Configuration;

namespace Rag.Infrastructure.LLMProviders;

public class LLMProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly EmbeddingConfiguration _embeddingConfig;

    public LLMProviderFactory(IConfiguration configuration, ILoggerFactory loggerFactory, EmbeddingConfiguration embeddingConfig)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _embeddingConfig = embeddingConfig;
    }

    public ILLMProvider CreateProvider()
    {
        var provider = _configuration["LLMProvider:Provider"] ?? "Ollama";

        if (provider.Equals("Xinference", StringComparison.OrdinalIgnoreCase))
        {
            var section = _configuration.GetSection("Xinference");
            var config = new XinferenceConfiguration
            {
                BaseUrl = section["BaseUrl"] ?? "http://localhost:9997",
                ModelUid = section["ModelUid"] ?? "bge-m3",
                ChatModelUid = section["ChatModelUid"] ?? section["ModelUid"] ?? "qwen2.5-instruct",
                ApiKey = section["ApiKey"] ?? "EMPTY",
                EmbeddingModelUid = section["EmbeddingModelUid"] ?? section["ModelUid"] ?? "bge-m3",
                Dimensions = int.Parse(section["Dimensions"] ?? _embeddingConfig.Dimensions.ToString()),
                NormalizeEmbeddings = bool.Parse(section["NormalizeEmbeddings"] ?? _embeddingConfig.NormalizeEmbeddings.ToString())
            };

            var httpClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {config.ApiKey}");
            var logger = _loggerFactory.CreateLogger<XinferenceProvider>();
            return new XinferenceProvider(httpClient, config, logger);
        }

        {
            var section = _configuration.GetSection("Ollama");
            var config = new OllamaConfiguration
            {
                BaseUrl = section["BaseUrl"] ?? "http://localhost:11434",
                ChatModel = section["ChatModel"] ?? "qwen3",
                EmbeddingModel = section["EmbeddingModel"] ?? _embeddingConfig.Model ?? "bge-m3"
            };

            var httpClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
            var logger = _loggerFactory.CreateLogger<OllamaProvider>();
            return new OllamaProvider(httpClient, config, logger);
        }
    }
}

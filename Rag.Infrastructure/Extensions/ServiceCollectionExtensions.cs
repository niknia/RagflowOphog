using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Qdrant.Client;
using Rag.Application.Handlers;
using Rag.Application.Interfaces;
using Rag.Application.Services;
using Rag.Application.SemanticKernel.Factories;
using Rag.Infrastructure.Chunking;
using Rag.Infrastructure.Configuration;
using Rag.Infrastructure.DocumentProcessing;
using Rag.Infrastructure.LLMProviders;
using Rag.Infrastructure.Persistence;
using Rag.Infrastructure.SearchEngines;
using Rag.Infrastructure.Services;
using Rag.Infrastructure.VectorStores;

namespace Rag.Infrastructure.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRagInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var vectorProvider = configuration["VectorDatabase:Provider"] ?? "Chroma";
        var searchProvider = configuration["Search:Provider"] ?? "ElasticSearch";

        var ollamaSection = configuration.GetSection("Ollama");
        var ollamaConfig = new OllamaConfiguration
        {
            BaseUrl = ollamaSection["BaseUrl"] ?? "http://localhost:11434",
            ChatModel = ollamaSection["ChatModel"] ?? "qwen3",
            EmbeddingModel = ollamaSection["EmbeddingModel"] ?? "bge-m3"
        };
        services.AddSingleton(ollamaConfig);
        services.AddSingleton<IOllamaConfiguration>(ollamaConfig);

        var embeddingSection = configuration.GetSection("Embedding");
        var embeddingConfig = new EmbeddingConfiguration
        {
            Model = embeddingSection["Model"] ?? "bge-m3",
            Dimensions = int.Parse(embeddingSection["Dimensions"] ?? "1024"),
            BatchSize = int.Parse(embeddingSection["BatchSize"] ?? "16"),
            NormalizeEmbeddings = bool.Parse(embeddingSection["NormalizeEmbeddings"] ?? "true")
        };
        services.AddSingleton(embeddingConfig);

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=rag.db";
        services.AddDbContext<RagDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IProcessingProgressRepository, ProcessingProgressRepository>();

        if (vectorProvider.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(_ =>
            {
                var qdrantUrl = configuration["Qdrant:BaseUrl"] ?? "http://localhost:6333";
                var uri = new Uri(qdrantUrl);
                return new QdrantClient(uri.Host, uri.Port);
            });
            services.AddSingleton<IVectorStore, QdrantVectorStore>();
        }
        else
        {
            var chromaSection = configuration.GetSection("Chroma");
            var chromaConfig = new ChromaConfiguration
            {
                BaseUrl = chromaSection["BaseUrl"] ?? "http://localhost:8000"
            };
            services.AddSingleton(chromaConfig);
            services.AddSingleton<IVectorStore>(sp =>
            {
                var cfg = sp.GetRequiredService<ChromaConfiguration>();
                var httpClient = new HttpClient { BaseAddress = new Uri(cfg.BaseUrl) };
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChromaVectorStore>>();
                return new ChromaVectorStore(httpClient, logger);
            });
        }

        var elasticUrl = configuration["ElasticSearch:Url"] ?? "http://localhost:9200";
        var settings = new ConnectionSettings(new Uri(elasticUrl)).DefaultIndex("knowledge-index");
        services.AddSingleton<IElasticClient>(new ElasticClient(settings));

        if (searchProvider.Equals("OpenSearch", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<ISearchEngine, OpenSearchEngine>();
        else
            services.AddSingleton<ISearchEngine, ElasticSearchEngine>();

        services.AddSingleton<LLMProviderFactory>();
        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var factory = sp.GetRequiredService<LLMProviderFactory>();
            var provider = factory.CreateProvider();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EmbeddingService>>();
            return new EmbeddingService(provider, embeddingConfig, logger) as IEmbeddingService;
        });

        services.AddSingleton<XinferenceChatService>(sp =>
        {
            var config = new XinferenceConfiguration
            {
                BaseUrl = configuration["Xinference:BaseUrl"] ?? "http://localhost:9997",
                ChatModelUid = configuration["Xinference:ChatModelUid"] ?? configuration["Xinference:ModelUid"] ?? "qwen2.5-instruct",
                ApiKey = configuration["Xinference:ApiKey"] ?? "EMPTY"
            };
            var httpClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
            if (config.ApiKey != "EMPTY")
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {config.ApiKey}");
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<XinferenceChatService>>();
            return new XinferenceChatService(httpClient, config, logger);
        });

        services.AddScoped<IChunkingService, HybridChunkingService>();
        services.AddScoped<DocumentProcessor>();
        services.AddScoped<TextExtractor>();
        services.AddScoped<IRetrievalService, HybridRetrievalService>();
        services.AddScoped<IRerankingService, RerankingService>();
        services.AddScoped<ChatService>();
        services.AddSingleton<IKernelFactory, KernelFactory>();

        return services;
    }

    public static IServiceCollection AddRagApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UploadDocumentHandler).Assembly));
        return services;
    }
}

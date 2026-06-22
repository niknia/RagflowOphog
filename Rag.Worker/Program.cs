using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rag.Infrastructure.Extensions;
using Rag.Infrastructure.Persistence;
using Rag.Worker.BackgroundJobs;
using Rag.Worker.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var cs = context.Configuration["ConnectionStrings:DefaultConnection"] ?? "unknown";
        Console.WriteLine($"[Worker] Database: {cs}");
        Console.WriteLine($"[Worker] VectorDB: {context.Configuration["VectorDatabase:Provider"]} @ {context.Configuration["Chroma:BaseUrl"] ?? context.Configuration["Qdrant:BaseUrl"]}");
        Console.WriteLine($"[Worker] Search: {context.Configuration["Search:Provider"]} @ {context.Configuration["ElasticSearch:Url"]}");
        Console.WriteLine($"[Worker] Ollama: {context.Configuration["Ollama:BaseUrl"]}");

        services.AddRagInfrastructure(context.Configuration);
        services.AddRagApplication();

        services.AddSingleton<DocumentProcessingChannel>();
        services.AddHostedService<DocumentProcessingWorker>();
        services.AddHostedService<DocumentProcessingHostedService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
    db.Database.EnsureCreated();
}

await host.RunAsync();

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rag.Infrastructure.Extensions;
using Rag.Infrastructure.Persistence;
using Rag.Worker.BackgroundJobs;
using Rag.Worker.Services;
using Serilog;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\logs", "worker-error-.log"),
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}")
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            var cs = context.Configuration["ConnectionStrings:DefaultConnection"] ?? "unknown";
            Log.Information("Database: {ConnectionString}", cs);
            Log.Information("VectorDB: {Provider} @ {Url}",
                context.Configuration["VectorDatabase:Provider"],
                context.Configuration["Chroma:BaseUrl"] ?? context.Configuration["Qdrant:BaseUrl"]);
            Log.Information("Search: {Provider} @ {Url}",
                context.Configuration["Search:Provider"],
                context.Configuration["ElasticSearch:Url"]);
            Log.Information("Ollama: {BaseUrl}", context.Configuration["Ollama:BaseUrl"]);

            services.AddRagInfrastructure(context.Configuration);
            services.AddRagApplication();

            services.AddSingleton<DocumentProcessingChannel>();
            services.AddHostedService<DocumentProcessingWorker>();
            services.AddHostedService<DocumentProcessingHostedService>();
        })
        .UseSerilog()
        .Build();

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
        db.Database.EnsureCreated();
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

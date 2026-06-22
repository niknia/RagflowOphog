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

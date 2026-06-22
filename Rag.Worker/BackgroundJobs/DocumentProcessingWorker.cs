using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rag.Application.Interfaces;
using Rag.Contracts.Commands;
using Rag.Domain.Entities;
using Rag.Domain.Enums;
using Rag.Domain.ValueObjects;
using Rag.Infrastructure.DocumentProcessing;
using Rag.Infrastructure.Services;
using Rag.Worker.Services;

namespace Rag.Worker.BackgroundJobs;
public class DocumentProcessingWorker : BackgroundService
{
    private readonly DocumentProcessingChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        DocumentProcessingChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingWorker> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Worker started");

        await foreach (var command in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessDocumentAsync(command, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {DocumentId}", command.DocumentId);
            }
        }
    }

    private async Task ProcessDocumentAsync(ProcessDocumentCommand command, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var documentRepository = sp.GetRequiredService<IDocumentRepository>();
        var vectorStore = sp.GetRequiredService<IVectorStore>();
        var searchEngine = sp.GetRequiredService<ISearchEngine>();
        var embeddingService = sp.GetRequiredService<IEmbeddingService>();
        var documentProcessor = sp.GetRequiredService<DocumentProcessor>();
        var textExtractor = sp.GetRequiredService<TextExtractor>();
        var progressRepo = sp.GetRequiredService<IProcessingProgressRepository>();

        var document = await documentRepository.GetByIdAsync(command.DocumentId, ct);
        if (document == null)
        {
            _logger.LogWarning("Document {Id} not found", command.DocumentId);
            return;
        }

        _logger.LogInformation("Processing document {Id}: {Name}", document.Id, document.FileName);

        try
        {
            document.Status = DocumentStatus.Processing;
            await documentRepository.UpdateAsync(document, ct);

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 0, "Queued", "Document queued for processing", ct);

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 10, "Extracting", "Extracting text from document", ct);

            await using var fileStream = File.OpenRead(document.FilePath);
            var text = await textExtractor.ExtractTextAsync(fileStream, document.FileType, ct);
            document.OriginalContent = text;
            document.CleanedContent = text;

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 25, "Chunking", "Splitting document into chunks", ct);

            var chunks = await documentProcessor.ProcessDocumentAsync(document, new ChunkingOptions(), ct);
            if (chunks.Count == 0)
            {
                _logger.LogWarning("No chunks generated for document {Id}", document.Id);
                await ReportProgressAsync(progressRepo, document.Id, document.FileName, 100, "Failed", "No chunks generated", ct);
                return;
            }

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 40, "Embedding", $"Generating embeddings for {chunks.Count} chunks", ct);

            var collectionName = document.CollectionName;
            if (!await vectorStore.CollectionExistsAsync(collectionName, ct))
                await vectorStore.CreateCollectionAsync(collectionName, ct: ct);

            var batchSize = 16;
            for (int i = 0; i < chunks.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                var batch = chunks.Skip(i).Take(batchSize).ToList();
                var currentBatch = i / batchSize + 1;
                var totalBatches = (int)Math.Ceiling((double)chunks.Count / batchSize);
                var percent = 40 + (int)(35.0 * (i + batch.Count) / chunks.Count);

                await ReportProgressAsync(progressRepo, document.Id, document.FileName, percent, "Embedding",
                    $"Embedding batch {currentBatch}/{totalBatches}", ct);

                var embeddings = await embeddingService.GenerateEmbeddingsAsync(batch.Select(c => c.Content), ct);

                await ReportProgressAsync(progressRepo, document.Id, document.FileName, percent + 5, "Indexing",
                    $"Indexing batch {currentBatch}/{totalBatches} in vector store", ct);

                var vectorItems = new List<(string Id, float[] Embedding, string Metadata)>();
                var searchItems = new List<(string Id, string Text, Dictionary<string, object> Metadata)>();

                for (int j = 0; j < batch.Count; j++)
                {
                    var chunk = batch[j];
                    var embedding = embeddings[j];
                    var metadata = JsonSerializer.Serialize(new
                    {
                        DocumentId = document.Id.ToString(),
                        CollectionId = document.CollectionId,
                        ChunkIndex = chunk.ChunkIndex,
                        PageNumber = chunk.PageNumber,
                        Section = chunk.Section
                    });

                    chunk.Embedding = embedding;
                    chunk.VectorId = chunk.Id.ToString();
                    vectorItems.Add((chunk.Id.ToString(), embedding, metadata));
                    searchItems.Add((chunk.Id.ToString(), chunk.Content, new Dictionary<string, object>
                    {
                        ["DocumentId"] = document.Id.ToString(),
                        ["CollectionId"] = document.CollectionId,
                        ["ChunkIndex"] = chunk.ChunkIndex,
                        ["PageNumber"] = chunk.PageNumber
                    }));
                }

                await vectorStore.BatchInsertAsync(collectionName, vectorItems, ct);
                await searchEngine.BulkIndexAsync("knowledge-index", searchItems, ct);
            }

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 95, "Finalizing", "Finalizing document processing", ct);

            document.Status = DocumentStatus.Ready;
            document.ProcessedAt = DateTime.UtcNow;
            document.ChunkCount = chunks.Count;
            await documentRepository.UpdateAsync(document, ct);

            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 100, "Completed",
                $"Document processed: {chunks.Count} chunks indexed", ct);

            _logger.LogInformation("Document {Id} processed: {Chunks} chunks", document.Id, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            document.RetryCount++;
            await documentRepository.UpdateAsync(document, ct);
            await ReportProgressAsync(progressRepo, document.Id, document.FileName, 100, "Failed",
                $"Error: {ex.Message}", ct);
        }
    }

    private static async Task ReportProgressAsync(
        IProcessingProgressRepository repo, Guid documentId, string fileName,
        int percent, string stage, string message, CancellationToken ct)
    {
        try
        {
            var existing = await repo.GetByDocumentIdAsync(documentId, ct);
            if (existing != null)
            {
                existing.Percent = percent;
                existing.Stage = stage;
                existing.Message = message;
                existing.Status = percent >= 100 ? "Completed" : stage == "Failed" ? "Failed" : "Processing";
                existing.UpdatedAt = DateTime.UtcNow;
                if (percent >= 100 || stage == "Failed")
                    existing.CompletedAt = DateTime.UtcNow;
                await repo.UpdateAsync(existing, ct);
            }
            else
            {
                await repo.AddAsync(new ProcessingProgress
                {
                    DocumentId = documentId,
                    FileName = fileName,
                    Percent = percent,
                    Stage = stage,
                    Message = message,
                    Status = percent >= 100 ? "Completed" : "Processing",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);
            }
        }
        catch (Exception)
        {
            // Don't let progress reporting failure break the processing pipeline
        }
    }
}

public class DocumentProcessingHostedService : IHostedService
{
    private readonly DocumentProcessingChannel _channel;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentProcessingHostedService> _logger;

    public DocumentProcessingHostedService(
        DocumentProcessingChannel channel,
        IDocumentRepository documentRepository,
        ILogger<DocumentProcessingHostedService> logger)
    {
        _channel = channel;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pendingDocs = await _documentRepository.GetPendingDocumentsAsync(50, cancellationToken);
        foreach (var doc in pendingDocs)
        {
            await _channel.WriteAsync(new ProcessDocumentCommand { DocumentId = doc.Id }, cancellationToken);
        }
        _logger.LogInformation("Queued {Count} pending documents for processing", pendingDocs.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

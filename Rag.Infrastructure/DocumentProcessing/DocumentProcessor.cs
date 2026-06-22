using Microsoft.Extensions.Logging;
using Rag.Domain.Entities;
using Rag.Domain.Enums;
using Rag.Domain.ValueObjects;
using Rag.Infrastructure.Chunking;
using Rag.Application.Interfaces;
using Rag.Shared.Extensions;

namespace Rag.Infrastructure.DocumentProcessing;
public class DocumentProcessor
{
    private readonly IChunkingService _chunkingService;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(IChunkingService chunkingService, IDocumentRepository documentRepository, ILogger<DocumentProcessor> logger)
    {
        _chunkingService = chunkingService;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<List<Chunk>> ProcessDocumentAsync(Document document, ChunkingOptions? options = null, CancellationToken ct = default)
    {
        options ??= new ChunkingOptions();
        var text = document.CleanedContent;
        if (string.IsNullOrWhiteSpace(text))
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = "No text content to process";
            await _documentRepository.UpdateAsync(document, ct);
            return new List<Chunk>();
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await _documentRepository.UpdateAsync(document, ct);

            var detectedLanguage = DetectLanguage(text);
            document.DetectedLanguage = detectedLanguage;

            var chunkResults = await _chunkingService.ChunkAsync(text, options, ct);
            var chunks = chunkResults.Select((cr, idx) => new Chunk
            {
                DocumentId = document.Id,
                CollectionId = document.CollectionId,
                ChunkIndex = cr.Index,
                Content = cr.Content,
                TokenCount = cr.TokenCount,
                PageNumber = cr.PageNumber,
                Section = cr.Section,
                Language = detectedLanguage,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            document.ChunkCount = chunks.Count;
            document.Status = DocumentStatus.Ready;
            document.ProcessedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document, ct);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            document.RetryCount++;
            await _documentRepository.UpdateAsync(document, ct);
            return new List<Chunk>();
        }
    }

    private static Language DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Language.Unknown;
        bool hasPersian = text.Any(c => c >= 0x0600 && c <= 0x06FF);
        bool hasEnglish = text.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        if (hasPersian && hasEnglish) return Language.Mixed;
        if (hasPersian) return Language.Persian;
        if (hasEnglish) return Language.English;
        return Language.Unknown;
    }
}

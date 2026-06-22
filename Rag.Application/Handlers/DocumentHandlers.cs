using MediatR;
using Microsoft.Extensions.Logging;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.Entities;
using Rag.Domain.Enums;
using Rag.Application.Interfaces;
using Rag.Domain.ValueObjects;

namespace Rag.Application.Handlers;
public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, DocumentUploadResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProcessingProgressRepository _progressRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UploadDocumentHandler> _logger;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".docx", ".txt", ".csv", ".json", ".xml", ".html", ".htm", ".md", ".rtf" };

    public UploadDocumentHandler(
        IDocumentRepository documentRepository,
        IProcessingProgressRepository progressRepository,
        IMediator mediator,
        ILogger<UploadDocumentHandler> logger)
    {
        _documentRepository = documentRepository;
        _progressRepository = progressRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var ext = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not supported");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        Directory.CreateDirectory(uploadsDir);
        var filePath = Path.Combine(uploadsDir, $"{Guid.NewGuid()}{ext}");

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await request.FileStream.CopyToAsync(fileStream, ct);
        }

        var fileType = ext.ToLower() switch
        {
            ".pdf" => DocumentFileType.Pdf,
            ".docx" => DocumentFileType.Docx,
            ".txt" => DocumentFileType.Txt,
            ".csv" => DocumentFileType.Csv,
            ".json" => DocumentFileType.Json,
            ".xml" => DocumentFileType.Xml,
            ".html" or ".htm" => DocumentFileType.Html,
            ".md" => DocumentFileType.Markdown,
            ".rtf" => DocumentFileType.Rtf,
            _ => DocumentFileType.Txt
        };

        var document = new Document
        {
            FileName = request.FileName,
            FilePath = filePath,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            FileType = fileType,
            Status = DocumentStatus.Pending,
            CollectionId = request.CollectionId,
            CollectionName = request.CollectionName,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepository.AddAsync(document, ct);

        var progress = new ProcessingProgress
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            Percent = 0,
            Stage = "Uploaded",
            Message = "Document uploaded and queued for processing",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _progressRepository.AddAsync(progress, ct);

        await _mediator.Publish(new DocumentUploadedNotification(document.Id, document.FileName), ct);

        _logger.LogInformation("Document {Id} uploaded: {Name}", document.Id, document.FileName);

        return new DocumentUploadResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            Status = document.Status,
            Message = "Document uploaded and queued for processing"
        };
    }
}

public class GetDocumentsHandler : IRequestHandler<GetDocumentsQuery, DocumentListResponse>
{
    private readonly IDocumentRepository _repository;
    public GetDocumentsHandler(IDocumentRepository repository) => _repository = repository;
    public async Task<DocumentListResponse> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var docs = await _repository.GetAllAsync(request.Page, request.PageSize, request.Status, request.CollectionId, ct);
        var total = await _repository.CountAsync(request.Status, request.CollectionId, ct);
        return new DocumentListResponse
        {
            Documents = docs.Select(d => new DocumentDto
            {
                Id = d.Id, FileName = d.FileName, ContentType = d.ContentType,
                FileSize = d.FileSize, Status = d.Status, CollectionName = d.CollectionName,
                DetectedLanguage = d.DetectedLanguage, ChunkCount = d.ChunkCount,
                ErrorMessage = d.ErrorMessage, CreatedAt = d.CreatedAt, ProcessedAt = d.ProcessedAt
            }).ToList(),
            TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }
}

public class GetDocumentHandler : IRequestHandler<GetDocumentQuery, DocumentDto?>
{
    private readonly IDocumentRepository _repository;
    public GetDocumentHandler(IDocumentRepository repository) => _repository = repository;
    public async Task<DocumentDto?> Handle(GetDocumentQuery request, CancellationToken ct)
    {
        var d = await _repository.GetByIdAsync(request.Id, ct);
        return d == null ? null : new DocumentDto
        {
            Id = d.Id, FileName = d.FileName, ContentType = d.ContentType,
            FileSize = d.FileSize, Status = d.Status, CollectionName = d.CollectionName,
            DetectedLanguage = d.DetectedLanguage, ChunkCount = d.ChunkCount,
            ErrorMessage = d.ErrorMessage, CreatedAt = d.CreatedAt, ProcessedAt = d.ProcessedAt
        };
    }
}

public class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand, bool>
{
    private readonly IDocumentRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly ISearchEngine _searchEngine;
    public DeleteDocumentHandler(IDocumentRepository repository, IVectorStore vectorStore, ISearchEngine searchEngine)
    {
        _repository = repository; _vectorStore = vectorStore; _searchEngine = searchEngine;
    }
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.Id, ct);
        return true;
    }
}

public class ReindexDocumentHandler : IRequestHandler<ReindexDocumentCommand, bool>
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<ReindexDocumentHandler> _logger;
    public ReindexDocumentHandler(IDocumentRepository repository, ILogger<ReindexDocumentHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    public async Task<bool> Handle(ReindexDocumentCommand request, CancellationToken ct)
    {
        var doc = await _repository.GetByIdAsync(request.Id, ct);
        if (doc == null) return false;
        doc.Status = DocumentStatus.Pending;
        doc.ErrorMessage = string.Empty;
        await _repository.UpdateAsync(doc, ct);
        _logger.LogInformation("Document {Id} queued for reindexing", request.Id);
        return true;
    }
}

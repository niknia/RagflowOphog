using MediatR;
using Rag.Contracts.DTOs;
using Rag.Domain.Enums;

namespace Rag.Contracts.Commands;

public record DocumentUploadedNotification(Guid DocumentId, string FileName) : INotification;
public class UploadDocumentCommand : IRequest<DocumentUploadResponse>
{
    public Stream FileStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public string CollectionId { get; set; } = "default";
    public string CollectionName { get; set; } = "knowledge-base";
    public string ChunkingStrategy { get; set; } = "Hybrid";
    public int MaxChunkSize { get; set; } = 1024;
    public int ChunkOverlap { get; set; } = 128;
    public int EmbeddingDimensions { get; set; } = 1024;
    public bool NormalizeEmbeddings { get; set; } = true;
}

public class DeleteDocumentCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class ReindexDocumentCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class RebuildCollectionCommand : IRequest<bool>
{
    public string CollectionId { get; set; } = string.Empty;
}

public class ProcessDocumentCommand : IRequest<bool>
{
    public Guid DocumentId { get; set; }
}

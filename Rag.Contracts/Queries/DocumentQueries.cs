using MediatR;
using Rag.Contracts.DTOs;

namespace Rag.Contracts.Queries;
public class GetDocumentQuery : IRequest<DocumentDto?>
{
    public Guid Id { get; set; }
}

public class GetDocumentsQuery : IRequest<DocumentListResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? CollectionId { get; set; }
}

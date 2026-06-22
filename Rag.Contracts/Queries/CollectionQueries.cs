using MediatR;
using Rag.Contracts.DTOs;

namespace Rag.Contracts.Queries;
public class GetCollectionsQuery : IRequest<List<CollectionDto>>
{
}

public class GetCollectionQuery : IRequest<CollectionDto?>
{
    public string Id { get; set; } = string.Empty;
}

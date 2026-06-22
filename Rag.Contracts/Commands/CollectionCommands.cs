using MediatR;
using Rag.Contracts.DTOs;

namespace Rag.Contracts.Commands;
public class CreateCollectionCommand : IRequest<CollectionDto>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VectorDimension { get; set; } = 1024;
}

public class DeleteCollectionCommand : IRequest<bool>
{
    public string Id { get; set; } = string.Empty;
}

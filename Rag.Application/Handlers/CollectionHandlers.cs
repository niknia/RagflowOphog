using MediatR;
using Rag.Contracts.Commands;
using Rag.Contracts.DTOs;
using Rag.Contracts.Queries;
using Rag.Domain.Entities;
using Rag.Application.Interfaces;

namespace Rag.Application.Handlers;
public class CreateCollectionHandler : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _repository;
    private readonly IVectorStore _vectorStore;
    public CreateCollectionHandler(ICollectionRepository repository, IVectorStore vectorStore)
    {
        _repository = repository; _vectorStore = vectorStore;
    }
    public async Task<CollectionDto> Handle(CreateCollectionCommand request, CancellationToken ct)
    {
        var collection = new Collection
        {
            Name = request.Name, Description = request.Description, VectorDimension = request.VectorDimension
        };
        await _vectorStore.CreateCollectionAsync(collection.Name, request.VectorDimension, ct);
        collection = await _repository.CreateAsync(collection, ct);
        return new CollectionDto
        {
            Id = collection.Id, Name = collection.Name, Description = collection.Description,
            VectorDimension = collection.VectorDimension, CreatedAt = collection.CreatedAt
        };
    }
}

public class GetCollectionsHandler : IRequestHandler<GetCollectionsQuery, List<CollectionDto>>
{
    private readonly ICollectionRepository _repository;
    public GetCollectionsHandler(ICollectionRepository repository) => _repository = repository;
    public async Task<List<CollectionDto>> Handle(GetCollectionsQuery request, CancellationToken ct)
    {
        var collections = await _repository.GetAllAsync(ct);
        return collections.Select(c => new CollectionDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description,
            DocumentCount = c.DocumentCount, ChunkCount = c.ChunkCount, CreatedAt = c.CreatedAt
        }).ToList();
    }
}

public class DeleteCollectionHandler : IRequestHandler<DeleteCollectionCommand, bool>
{
    private readonly ICollectionRepository _repository;
    private readonly IVectorStore _vectorStore;
    public DeleteCollectionHandler(ICollectionRepository repository, IVectorStore vectorStore)
    {
        _repository = repository; _vectorStore = vectorStore;
    }
    public async Task<bool> Handle(DeleteCollectionCommand request, CancellationToken ct)
    {
        var collection = await _repository.GetByIdAsync(request.Id, ct);
        if (collection != null)
        {
            await _vectorStore.DeleteCollectionAsync(collection.Name, ct);
            await _repository.DeleteAsync(request.Id, ct);
        }
        return true;
    }
}

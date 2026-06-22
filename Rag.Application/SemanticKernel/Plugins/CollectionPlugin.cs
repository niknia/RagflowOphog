using System.ComponentModel;
using Microsoft.SemanticKernel;
using Rag.Application.Interfaces;

namespace Rag.Application.SemanticKernel.Plugins;
public class CollectionPlugin
{
    private readonly ICollectionRepository _collectionRepository;

    public CollectionPlugin(ICollectionRepository collectionRepository) => _collectionRepository = collectionRepository;

    [KernelFunction("list_collections")]
    [Description("List all available document collections")]
    public async Task<string> ListCollectionsAsync(CancellationToken ct = default)
    {
        var collections = await _collectionRepository.GetAllAsync(ct);
        return string.Join("\n", collections.Select(c => $"- {c.Name} ({c.DocumentCount} documents)"));
    }
}

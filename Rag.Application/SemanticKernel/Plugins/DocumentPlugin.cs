using System.ComponentModel;
using Microsoft.SemanticKernel;
using Rag.Application.Interfaces;

namespace Rag.Application.SemanticKernel.Plugins;
public class DocumentPlugin
{
    private readonly IDocumentRepository _documentRepository;

    public DocumentPlugin(IDocumentRepository documentRepository) => _documentRepository = documentRepository;

    [KernelFunction("get_document_count")]
    [Description("Get the total number of documents in the system")]
    public async Task<int> GetDocumentCountAsync(CancellationToken ct = default)
        => await _documentRepository.CountAsync(ct: ct);

    [KernelFunction("get_document_status")]
    [Description("Get the processing status of a document")]
    public async Task<string> GetDocumentStatusAsync(
        [Description("The document ID")] Guid documentId,
        CancellationToken ct = default)
    {
        var doc = await _documentRepository.GetByIdAsync(documentId, ct);
        return doc?.Status.ToString() ?? "Not Found";
    }
}

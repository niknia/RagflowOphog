using MediatR;
using Microsoft.Extensions.Logging;
using Rag.Contracts.Commands;
using Rag.Worker.Services;

namespace Rag.Worker.Handlers;

public class DocumentUploadedHandler : INotificationHandler<DocumentUploadedNotification>
{
    private readonly DocumentProcessingChannel _channel;
    private readonly ILogger<DocumentUploadedHandler> _logger;

    public DocumentUploadedHandler(DocumentProcessingChannel channel, ILogger<DocumentUploadedHandler> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public async Task Handle(DocumentUploadedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queueing document {Id} for processing", notification.DocumentId);
        await _channel.WriteAsync(new ProcessDocumentCommand { DocumentId = notification.DocumentId }, cancellationToken);
    }
}

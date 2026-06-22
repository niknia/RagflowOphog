using Microsoft.AspNetCore.SignalR;

namespace Rag.Api.Hubs;

public class ProcessingHub : Hub
{
    public async Task JoinDocumentGroup(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doc-{documentId}");
    }

    public async Task LeaveDocumentGroup(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doc-{documentId}");
    }
}

using System.Threading.Channels;
using Rag.Contracts.Commands;

namespace Rag.Worker.Services;
public class DocumentProcessingChannel
{
    private readonly Channel<ProcessDocumentCommand> _channel;

    public DocumentProcessingChannel()
    {
        _channel = Channel.CreateBounded<ProcessDocumentCommand>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });
    }

    public async Task WriteAsync(ProcessDocumentCommand command, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(command, ct);

    public IAsyncEnumerable<ProcessDocumentCommand> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    public bool TryCompleteWriter() => _channel.Writer.TryComplete();
}

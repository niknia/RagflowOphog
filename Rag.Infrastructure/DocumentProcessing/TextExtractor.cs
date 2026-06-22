using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Rag.Domain.Enums;

namespace Rag.Infrastructure.DocumentProcessing;
public partial class TextExtractor
{
    private readonly ILogger<TextExtractor> _logger;

    public TextExtractor(ILogger<TextExtractor> logger) => _logger = logger;

    public async Task<string> ExtractTextAsync(Stream fileStream, DocumentFileType fileType, CancellationToken ct = default)
    {
        return fileType switch
        {
            DocumentFileType.Txt => await ExtractTxtAsync(fileStream, ct),
            DocumentFileType.Csv => await ExtractCsvAsync(fileStream, ct),
            DocumentFileType.Json => await ExtractJsonAsync(fileStream, ct),
            DocumentFileType.Xml => await ExtractXmlAsync(fileStream, ct),
            DocumentFileType.Html => await ExtractHtmlAsync(fileStream, ct),
            DocumentFileType.Markdown => await ExtractTxtAsync(fileStream, ct),
            DocumentFileType.Rtf => await ExtractTxtAsync(fileStream, ct),
            DocumentFileType.Pdf => await ExtractTxtAsync(fileStream, ct),
            DocumentFileType.Docx => await ExtractTxtAsync(fileStream, ct),
            _ => await ExtractTxtAsync(fileStream, ct)
        };
    }

    private static async Task<string> ExtractTxtAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(ct);
    }

    private static async Task<string> ExtractCsvAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var sb = new StringBuilder();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line != null) sb.AppendLine(StripHtmlRegex().Replace(line, " "));
        }
        return sb.ToString();
    }

    private static async Task<string> ExtractJsonAsync(Stream stream, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ExtractJsonValue(doc.RootElement);
    }

    private static string ExtractJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => string.Join(" ", element.EnumerateObject().Select(p => ExtractJsonValue(p.Value))),
        JsonValueKind.Array => string.Join(" ", element.EnumerateArray().Select(ExtractJsonValue)),
        JsonValueKind.String => element.GetString() ?? "",
        _ => element.ToString()
    };

    private static async Task<string> ExtractXmlAsync(Stream stream, CancellationToken ct)
    {
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        return string.Join(" ", doc.Descendants().Where(e => !e.HasElements).Select(e => e.Value));
    }

    private static async Task<string> ExtractHtmlAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var html = await reader.ReadToEndAsync(ct);
        return StripHtmlRegex().Replace(html, " ");
    }

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex StripHtmlRegex();
}

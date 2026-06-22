using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Rag.Application.SemanticKernel.Plugins;
public class CitationPlugin
{
    [KernelFunction("format_citations")]
    [Description("Format search results as proper citations with source information")]
    public string FormatCitations([Description("JSON array of citation objects")] string citationsJson)
    {
        var citations = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(citationsJson);
        if (citations == null || citations.Count == 0) return "No sources cited.";
        var sb = new StringBuilder();
        sb.AppendLine("\n--- Sources ---");
        for (int i = 0; i < citations.Count; i++)
        {
            var c = citations[i];
            sb.AppendLine($"{i + 1}. {c.GetValueOrDefault("DocumentName", "")}");
            sb.AppendLine($"   Collection: {c.GetValueOrDefault("Collection", "")}");
            sb.AppendLine($"   Page: {c.GetValueOrDefault("PageNumber", "")}, Chunk: {c.GetValueOrDefault("ChunkNumber", "")}");
            sb.AppendLine($"   Score: {c.GetValueOrDefault("Score", "")}");
            sb.AppendLine($"   Type: {c.GetValueOrDefault("SearchType", "")}");
        }
        return sb.ToString();
    }
}

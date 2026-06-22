using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Rag.Domain.Enums;
using Rag.Domain.ValueObjects;
using Rag.Shared.Constants;
using Rag.Shared.Extensions;

namespace Rag.Infrastructure.Chunking;
public partial class HybridChunkingService : IChunkingService
{
    private readonly ILogger<HybridChunkingService> _logger;
    private static readonly Regex SectionRegex = SectionPatternRegex();
    private static readonly Regex PageBreakRegex = PageBreakPatternRegex();
    private static readonly Regex SentenceEndRegex = SentenceEndPatternRegex();

    public HybridChunkingService(ILogger<HybridChunkingService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChunkResult>> ChunkAsync(string text, ChunkingOptions options, CancellationToken ct = default)
    {
        var chunks = new List<ChunkResult>();
        var sections = SplitBySections(text);
        int globalIndex = 0;
        int currentPage = 1;

        foreach (var (sectionName, sectionContent) in sections)
        {
            ct.ThrowIfCancellationRequested();
            var paragraphs = SplitByParagraphs(sectionContent);

            foreach (var paragraph in paragraphs)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var page in ParsePages(paragraph))
                {
                    currentPage = page.PageNumber;
                    var sentences = page.Content;
                    var currentChunk = new List<string>();
                    int currentLength = 0;

                    foreach (var sentence in SplitBySentences(sentences))
                    {
                        var cleaned = sentence.CleanText();
                        if (string.IsNullOrWhiteSpace(cleaned)) continue;
                        int tokenCount = cleaned.Length;

                        if (currentLength + tokenCount > options.MaxChunkSize && currentChunk.Count > 0)
                        {
                            chunks.Add(BuildChunk(currentChunk, globalIndex++, sectionName, currentPage));
                            currentChunk.Clear();
                            currentLength = 0;
                        }

                        currentChunk.Add(cleaned);
                        currentLength += tokenCount;
                    }

                    if (currentChunk.Count > 0)
                        chunks.Add(BuildChunk(currentChunk, globalIndex++, sectionName, currentPage));
                }
            }
        }

        return chunks;
    }

    private static ChunkResult BuildChunk(List<string> sentences, int index, string section, int page)
    {
        var content = string.Join(" ", sentences);
        return new ChunkResult
        {
            Content = content,
            Index = index,
            TokenCount = content.Length,
            PageNumber = page,
            Section = section
        };
    }

    private static List<(string Name, string Content)> SplitBySections(string text)
    {
        var sections = new List<(string, string)>();
        var matches = SectionRegex.Matches(text);
        int lastIndex = 0;
        string lastName = "general";

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Index > lastIndex)
                sections.Add((lastName, text[lastIndex..match.Index]));
            lastName = match.Groups[1].Value;
            lastIndex = match.Index;
        }
        if (lastIndex < text.Length)
            sections.Add((lastName, text[lastIndex..]));
        if (sections.Count == 0)
            sections.Add(("general", text));

        return sections;
    }

    private static List<string> SplitByParagraphs(string text)
        => text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

    private static List<(int PageNumber, string Content)> ParsePages(string text)
    {
        var pages = new List<(int, string)>();
        var matches = PageBreakRegex.Matches(text);
        int last = 0;
        int pageNum = 1;
        foreach (Match match in matches)
        {
            pages.Add((pageNum++, text[last..match.Index]));
            last = match.Index;
        }
        if (last < text.Length) pages.Add((pageNum, text[last..]));
        if (pages.Count == 0) pages.Add((1, text));
        return pages;
    }

    private static List<string> SplitBySentences(string text)
    {
        var sentences = new List<string>();
        int last = 0;
        foreach (Match match in SentenceEndRegex.Matches(text))
        {
            sentences.Add(text[last..(match.Index + match.Length)]);
            last = match.Index + match.Length;
        }
        if (last < text.Length) sentences.Add(text[last..]);
        return sentences.Count > 0 ? sentences : new List<string> { text };
    }

    [GeneratedRegex(@"(?:^|\n)((?:فصل|بخش|تبصره|مادة|Section|Chapter|Article|Appendix)\s+[\d\-]+\s*[:]?\s*)", RegexOptions.IgnoreCase)]
    private static partial Regex SectionPatternRegex();

    [GeneratedRegex(@"\f|\n---|\n___|\n\*\*\*")]
    private static partial Regex PageBreakPatternRegex();

    [GeneratedRegex(@"[.!?؟!\u060C\u061F]\s*")]
    private static partial Regex SentenceEndPatternRegex();
}

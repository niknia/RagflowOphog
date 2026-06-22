using Rag.Domain.Enums;

namespace Rag.Domain.ValueObjects;
public class ChunkingOptions
{
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Hybrid;
    public int MaxChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 64;
    public bool RespectParagraphBoundaries { get; set; } = true;
    public bool RespectSentenceBoundaries { get; set; } = true;
    public bool ExtractSections { get; set; } = true;
    public bool ExtractPageNumbers { get; set; } = true;
    public int MinChunkSize { get; set; } = 64;
}

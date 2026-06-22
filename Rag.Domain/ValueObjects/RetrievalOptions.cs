using Rag.Domain.Enums;

namespace Rag.Domain.ValueObjects;
public class RetrievalOptions
{
    public RetrievalMode Mode { get; set; } = RetrievalMode.Hybrid;
    public int VectorLimit { get; set; } = 10;
    public int KeywordLimit { get; set; } = 10;
    public int HybridLimit { get; set; } = 10;
    public int FinalLimit { get; set; } = 5;
    public double VectorWeight { get; set; } = 0.5;
    public double KeywordWeight { get; set; } = 0.5;
    public double ScoreThreshold { get; set; } = 0.0;
    public RerankingMode RerankingMode { get; set; } = RerankingMode.ScoreFusion;
    public bool EnableQueryExpansion { get; set; }
    public bool EnableMultiQuery { get; set; }
    public string CollectionId { get; set; } = "default";
    public string Language { get; set; } = string.Empty;
}

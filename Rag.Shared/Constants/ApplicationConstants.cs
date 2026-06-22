namespace Rag.Shared.Constants;
public static class ApplicationConstants
{
    public const string ApplicationName = "RagPlatform";
    public const string Version = "1.0.0";
    public const string DefaultCollection = "knowledge-base";
    public const string DefaultModel = "qwen3";
    public const string DefaultEmbeddingModel = "bge-m3";
    public const int DefaultChunkSize = 512;
    public const int DefaultChunkOverlap = 64;
    public const int DefaultVectorDimension = 1024;
    public const int MaxFileSize = 50 * 1024 * 1024;
    public const string SqliteFileName = "rag.db";
}

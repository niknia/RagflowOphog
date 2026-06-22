using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace Rag.Application.SemanticKernel.Plugins;
public partial class LanguagePlugin
{
    [KernelFunction("detect_language")]
    [Description("Detect whether text is Persian, English, or mixed")]
    public string DetectLanguage([Description("The text to analyze")] string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        bool hasPersian = PersianCharRegex().IsMatch(text);
        bool hasEnglish = EnglishCharRegex().IsMatch(text);
        if (hasPersian && hasEnglish) return "mixed";
        if (hasPersian) return "persian";
        if (hasEnglish) return "english";
        return "unknown";
    }

    [KernelFunction("translate_query")]
    [Description("Add Persian translations to English terms for better retrieval")]
    public string ExpandQuery([Description("The search query")] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return query;
        if (DetectLanguage(query) == "persian")
            return query;
        return $"{query} {query}";
    }

    [GeneratedRegex(@"[\u0600-\u06FF]")]
    private static partial Regex PersianCharRegex();

    [GeneratedRegex(@"[a-zA-Z]")]
    private static partial Regex EnglishCharRegex();
}

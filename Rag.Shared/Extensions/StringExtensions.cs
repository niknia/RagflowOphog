using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Rag.Shared.Constants;

namespace Rag.Shared.Extensions;
public static partial class StringExtensions
{
    public static string NormalizeUnicode(this string text)
    {
        text = text.Normalize(NormalizationForm.FormKC);
        text = text.Replace(PersianConstants.ArabicYeh, PersianConstants.PersianYeh);
        text = text.Replace(PersianConstants.ArabicKaf, PersianConstants.PersianKaf);
        return text;
    }

    public static string NormalizePersianDigits(this string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (PersianConstants.PersianDigits.Contains(c))
                sb.Append(char.GetNumericValue(c));
            else if (PersianConstants.ArabicDigits.Contains(c))
                sb.Append(char.GetNumericValue(c));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string CleanText(this string text)
    {
        text = text.NormalizeUnicode();
        text = text.NormalizePersianDigits();
        text = MultipleNewlinesRegex().Replace(text, "\n\n");
        text = MultipleSpacesRegex().Replace(text, " ");
        return text.Trim();
    }

    public static bool IsPersian(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        int persianCount = text.Count(c => c >= 0x0600 && c <= 0x06FF);
        return persianCount > text.Length * 0.3;
    }

    public static bool IsMixedLanguage(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        int persianCount = text.Count(c => c >= 0x0600 && c <= 0x06FF);
        int englishCount = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        return persianCount > 0 && englishCount > 0;
    }

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlinesRegex();

    [GeneratedRegex(@" {2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"\p{P}+$")]
    public static partial Regex TrailingPunctuationRegex();
}

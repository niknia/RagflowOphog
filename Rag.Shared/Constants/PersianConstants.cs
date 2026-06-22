using System.Globalization;

namespace Rag.Shared.Constants;
public static class PersianConstants
{
    public static readonly CultureInfo PersianCulture = new("fa-IR");
    public const string PersianLocale = "fa-IR";
    public const string EnglishLocale = "en-US";
    public const char ZeroWidthNonJoiner = '\u200C';
    public const char ArabicYeh = '\u064A';
    public const char PersianYeh = '\u06CC';
    public const char ArabicKaf = '\u0643';
    public const char PersianKaf = '\u06A9';
    public const char ArabicHamza = '\u0621';

    public static readonly HashSet<char> PersianDigits = new()
    {
        '\u06F0', '\u06F1', '\u06F2', '\u06F3', '\u06F4',
        '\u06F5', '\u06F6', '\u06F7', '\u06F8', '\u06F9'
    };

    public static readonly HashSet<char> ArabicDigits = new()
    {
        '\u0660', '\u0661', '\u0662', '\u0663', '\u0664',
        '\u0665', '\u0666', '\u0667', '\u0668', '\u0669'
    };

    public static readonly string[] PersianSections = { "فصل", "بخش", "تبصره", "ماده" };
    public static readonly string[] EnglishSections = { "Section", "Chapter", "Appendix", "Article" };
}

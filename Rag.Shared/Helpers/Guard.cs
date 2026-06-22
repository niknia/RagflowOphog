namespace Rag.Shared.Helpers;
public static class Guard
{
    public static T ThrowIfNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static string ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace", paramName);
        return value;
    }

    public static int ThrowIfNegativeOrZero(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive");
        return value;
    }
}

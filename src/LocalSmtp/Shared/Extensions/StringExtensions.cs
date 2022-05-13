namespace LocalSmtp.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhitespace(this string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }

    public static bool IsNullOrEmpty(this string text)
    {
        return string.IsNullOrEmpty(text);
    }

    public static string JoinString(this IEnumerable<string> items, string delimiter)
    {
        return string.Join(delimiter, items);
    }
}

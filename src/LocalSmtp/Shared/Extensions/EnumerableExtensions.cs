namespace LocalSmtp.Shared.Extensions;

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> items)
    {
        return items == null || !items.Any();
    }
}

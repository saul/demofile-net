namespace DemoFile.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
        where T : class =>
        enumerable.Where(x => x != null)!;
}
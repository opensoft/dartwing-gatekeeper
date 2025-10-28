namespace DartWing.DomainModel.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> query, bool condition,
        Func<T, bool> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }
}
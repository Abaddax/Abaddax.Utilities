namespace Abaddax.Utilities.Collections
{

    public static class EnumerableExtensions
    {
#if !NET9_0_OR_GREATER
        public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
            => source.Select((x, i) => (i, x));
#endif
    }
}

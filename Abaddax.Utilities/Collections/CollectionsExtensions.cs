namespace Abaddax.Utilities.Collections
{
    public static class CollectionsExtensions
    {
        public static void UpdateFrom<T>(this ICollection<T> collection, IEnumerable<T> values)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(values);
            collection.Clear();
            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        public static int RemoveWhere<T>(this ICollection<T> collection, Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(match);
            List<T> itemsToRemove = new();
            foreach (var item in collection)
            {
                if (match.Invoke(item))
                    itemsToRemove.Add(item);
            }
            foreach (var item in itemsToRemove)
            {
                collection.Remove(item);
            }
            return itemsToRemove.Count;
        }
    }
}

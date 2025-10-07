using System.Collections;
using System.Diagnostics;

namespace Abaddax.Utilities.Collections.Concurrent
{
    #region DebugView
    sealed class ConcurrentList_DebugView<T>
    {
        private readonly ConcurrentList<T> _list;
        public ConcurrentList_DebugView(ConcurrentList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            _list = list;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                if (_list.Count == 0)
                    return Array.Empty<T>();
                return _list.ToArray();
            }
        }
    }
    #endregion

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ConcurrentHashSet_DebugView<>))]
    public class ConcurrentList<T> : ConcurrentCollectionBase,
        IEnumerable, IEnumerable<T>,
        IReadOnlyCollection<T>, ICollection<T>,
        IReadOnlyList<T>, IList<T>
    {
        private readonly List<T> _list;

        public ConcurrentList() => _list = new List<T>();
        public ConcurrentList(int capacity) => _list = new List<T>(capacity);
        public ConcurrentList(IEnumerable<T> collection) => _list = new List<T>(collection);

        public int Count => WithReadLock(() => _list.Count);
        public int Capacity => WithWriteLock(() => _list.Capacity);

        public T this[int index]
        {
            get => WithReadLock(() => _list[index]);
            set => WithWriteLock(() => _list[index] = value);
        }

        public void Add(T item) => WithWriteLock(() => _list.Add(item));
        public void AddRange(IEnumerable<T> collection) => WithWriteLock(() => _list.AddRange(collection));
        public bool Remove(T item) => WithWriteLock(() => _list.Remove(item));
        public int RemoveAll(Predicate<T> match) => WithWriteLock(() => _list.RemoveAll(match));
        public void RemoveAt(int index) => WithWriteLock(() => _list.RemoveAt(index));
        public void RemoveRange(int index, int count) => WithWriteLock(() => _list.RemoveRange(index, count));
        public void Clear() => WithWriteLock(() => _list.Clear());
        public bool Contains(T item) => WithReadLock(() => _list.Contains(item));
        public int EnsureCapacity(int capacity) => WithWriteLock(() => _list.EnsureCapacity(capacity));
        public int IndexOf(T item) => WithReadLock(() => _list.IndexOf(item));
        public int BinarySearch(T item) => WithReadLock(() => _list.BinarySearch(item));
        public void Insert(int index, T item) => WithWriteLock(() => _list.Insert(index, item));
        public void InsertRange(int index, IEnumerable<T> collection) => WithWriteLock(() => _list.InsertRange(index, collection));
        public T[] ToArray() => WithReadLock(() => _list.ToArray());

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        #endregion

        #region IEnumerable<T>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
        #endregion

        #region ICollection<T>
        public bool IsReadOnly => (_list as ICollection<T>)?.IsReadOnly ?? true;
        public void CopyTo(T[] array, int arrayIndex) => WithReadLock(() => _list.CopyTo(array, arrayIndex));
        #endregion
    }
}

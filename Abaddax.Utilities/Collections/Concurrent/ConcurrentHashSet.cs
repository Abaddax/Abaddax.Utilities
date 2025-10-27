using System.Collections;
using System.Diagnostics;

namespace Abaddax.Utilities.Collections.Concurrent
{
    #region DebugView
    sealed class ConcurrentHashSet_DebugView<T>
    {
        private readonly ConcurrentHashSet<T> _hashSet;
        public ConcurrentHashSet_DebugView(ConcurrentHashSet<T> hashSet)
        {
            if (hashSet == null)
                throw new ArgumentNullException(nameof(hashSet));

            _hashSet = hashSet;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                if (_hashSet.Count == 0)
                    return Array.Empty<T>();
                return _hashSet.ToArray();
            }
        }
    }
    #endregion

    /// <summary>
    /// List that contains unique values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ConcurrentHashSet_DebugView<>))]
    public class ConcurrentHashSet<T> : ConcurrentCollectionBase,
        IEnumerable, IEnumerable<T>,
        IReadOnlyCollection<T>, ICollection<T>, ICollection,
        IReadOnlySet<T>, ISet<T>
    {
        private readonly HashSet<T> _hashSet;

        public ConcurrentHashSet(IEqualityComparer<T>? comparer = null) => _hashSet = new HashSet<T>(comparer);
        public ConcurrentHashSet(int capacity, IEqualityComparer<T>? comparer = null) => _hashSet = new HashSet<T>(capacity, comparer);
        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null) => _hashSet = new HashSet<T>(collection, comparer);

        public int Count => WithReadLock(() => _hashSet.Count);
#if NET9_0
        public int Capacity => WithWriteLock(() => _hashSet.Capacity);
#endif
        public IEqualityComparer<T> Comparer => _hashSet.Comparer;

        public bool Add(T item) => WithWriteLock(() => _hashSet.Add(item));
        public bool Remove(T item) => WithWriteLock(() => _hashSet.Remove(item));
        public bool Contains(T item) => WithReadLock(() => _hashSet.Contains(item));
        public void Clear() => WithWriteLock(() => _hashSet.Clear());
        public int EnsureCapacity(int capacity) => WithWriteLock(() => _hashSet.EnsureCapacity(capacity));


        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => _hashSet.GetEnumerator();
        #endregion

        #region IEnumerable<T>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _hashSet.GetEnumerator();
        #endregion

        #region ICollection
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index)
        {
            if (array is not T[] typedArray)
                throw new ArgumentException($"Must be of type {typeof(T)}", nameof(array));
            WithReadLock(() => _hashSet.CopyTo(typedArray, index));
        }
        #endregion

        #region ICollection<T>
        bool ICollection<T>.IsReadOnly => (_hashSet as ICollection<T>)?.IsReadOnly ?? true;
        void ICollection<T>.Add(T item) => WithWriteLock(() =>
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (!_hashSet.Add(item))
                throw new ArgumentException($"{nameof(item)} already exists");
            return;
        });
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => WithReadLock(() => _hashSet.CopyTo(array, arrayIndex));
        #endregion

        #region ISet<T>
        public void ExceptWith(IEnumerable<T> other) => WithWriteLock(() => _hashSet.ExceptWith(other));
        public void IntersectWith(IEnumerable<T> other) => WithWriteLock(() => _hashSet.IntersectWith(other));
        public void SymmetricExceptWith(IEnumerable<T> other) => WithWriteLock(() => _hashSet.SymmetricExceptWith(other));
        public void UnionWith(IEnumerable<T> other) => WithWriteLock(() => _hashSet.UnionWith(other));
        #endregion

        #region IReadOnlySet<T>
        public bool IsProperSubsetOf(IEnumerable<T> other) => WithReadLock(() => _hashSet.IsProperSubsetOf(other));
        public bool IsProperSupersetOf(IEnumerable<T> other) => WithReadLock(() => _hashSet.IsProperSupersetOf(other));
        public bool IsSubsetOf(IEnumerable<T> other) => WithReadLock(() => _hashSet.IsSubsetOf(other));
        public bool IsSupersetOf(IEnumerable<T> other) => WithReadLock(() => _hashSet.IsSupersetOf(other));
        public bool Overlaps(IEnumerable<T> other) => WithReadLock(() => _hashSet.Overlaps(other));
        public bool SetEquals(IEnumerable<T> other) => WithReadLock(() => _hashSet.SetEquals(other));
        #endregion
    }
}

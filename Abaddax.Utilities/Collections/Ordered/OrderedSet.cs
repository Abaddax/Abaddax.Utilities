using System.Collections;
using System.Diagnostics;

namespace Abaddax.Utilities.Collections.Ordered
{
    #region DebugView

    sealed class OrderedSet_DebugView<T>
    {
        private OrderedSet<T> _set;
        public OrderedSet_DebugView(OrderedSet<T> set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            _set = set;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                if (_set.Count == 0)
                    return Array.Empty<T>();
                return _set.ToArray();
            }
        }
    }
    #endregion

    /// <summary>
    /// List that contains unique values and guarantees the order of the entries
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(OrderedSet_DebugView<>))]
    public class OrderedSet<T> :
        IEnumerable, IEnumerable<T>,
        IReadOnlyCollection<T>, ICollection<T>,
        IReadOnlySet<T>
    {
        private readonly List<T> _set = new List<T>();

        public OrderedSet()
        {

        }
        public OrderedSet(IList<T> list)
        {
            if (list != null)
            {
                _set.AddRange(list);
            }
        }

        public int Count => _set.Count;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _set[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _set[index] = value;
            }
        }
        public bool Add(T item)
        {
            if (item == null)
                return false;
            if (_set.Contains(item))
                return false;
            _set.Add(item);
            return true;
        }
        public bool Remove(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (!_set.Contains(item))
                return false;
            _set.Remove(item);
            return true;
        }
        public void Clear()
        {
            _set.Clear();
        }

        public bool Contains(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return _set.Contains(item);
        }
        public int IndexOf(T item)
        {
            if (item == null)
                return -1;
            return _set.IndexOf(item);
        }

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
        #endregion

        #region IEnumerable<T>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _set.GetEnumerator();
        #endregion

        #region ICollection<T>
        bool ICollection<T>.IsReadOnly => (_set as ICollection<T>)?.IsReadOnly ?? true;
        void ICollection<T>.Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (_set.Contains(item))
                throw new ArgumentException($"{nameof(item)} already exists");
            _set.Add(item);
        }
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);
        #endregion

        #region IReadOnlySet<T>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.IsProperSubsetOf(other);
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.IsProperSupersetOf(other);
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.IsSubsetOf(other);
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.IsSupersetOf(other);
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.Overlaps(other);
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            var set = _set.ToHashSet();
            return set.SetEquals(other);
        }
        #endregion
    }
}

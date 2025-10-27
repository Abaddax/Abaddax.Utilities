using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Abaddax.Utilities.Collections.Ordered
{
#if !NET9_0_OR_GREATER

    #region DebugView

    sealed class OrderedDictionary_DebugView<TKey, TValue>
          where TKey : notnull
    {
        private readonly OrderedDictionary<TKey, TValue> _dict;
        public OrderedDictionary_DebugView(OrderedDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            _dict = dictionary;
        }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] KeyValuePairs
        {
            get
            {
                if (_dict.Count == 0)
                    return Array.Empty<KeyValuePair<TKey, TValue>>();
                return _dict.ToArray();
            }
        }
    }
    #endregion

    /// <summary>
    /// Dictionary that guarantees the order of the entries
    /// </summary>
    /// <remarks>Based on https://referencesource.microsoft.com/#System.ServiceModel.Internals/System/Runtime/Collections/OrderedDictionary.cs</remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(OrderedDictionary_DebugView<,>))]
    public class OrderedDictionary<TKey, TValue> :
        IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, //IEnumerable<TValue>,
        IReadOnlyCollection<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>,
        IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly OrderedDictionary _dictionary = new OrderedDictionary();

        public OrderedDictionary()
        {

        }
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            if (dictionary != null)
            {
                foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                {
                    _dictionary.Add(pair.Key, pair.Value);
                }
            }
        }

        public int Count => _dictionary.Count;

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (!_dictionary.Contains(key))
                    throw new KeyNotFoundException();
                return (TValue)_dictionary[key]!;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (!_dictionary.Contains(key))
                    Add(key, value);
                else
                    _dictionary[key] = value;
            }
        }
        public TValue this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return (TValue)_dictionary[index]!;
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _dictionary[index] = value;
            }
        }
        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (_dictionary.Contains(key))
                throw new ArgumentException("key already exists");
            _dictionary.Add(key, value);
        }
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!_dictionary.Contains(key))
                return false;
            _dictionary.Remove(key);
            return true;
        }
        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return _dictionary.Contains(key);
        }
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            bool found = ContainsKey(key);
            value = found ? this[key] : default;
            return found;
        }

        public int IndexOf(TKey key)
        {
            if (key == null)
                return -1;
            if (!_dictionary.Contains(key))
                return -1;
            int i = 0;
            foreach (var entry in _dictionary.Keys)
            {
                if (entry.Equals(key))
                    return i;
                i++;
            }
            return -1;
        }

        #region IDictionary<TKey, TValue>
        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> keys = new List<TKey>(_dictionary.Count);
                foreach (TKey key in _dictionary.Keys)
                {
                    keys.Add(key);
                }
                return keys.AsReadOnly();
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>(_dictionary.Count);
                foreach (TValue value in _dictionary.Values)
                {
                    values.Add(value);
                }
                return values.AsReadOnly();
            }
        }
        #endregion

        #region IReadOnlyDictionary<TKey, TValue>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
        #endregion

        #region IEnumerable<KeyValuePair<TKey, TValue>>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (DictionaryEntry entry in _dictionary)
            {
                yield return new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value!);
            }
        }
        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => _dictionary.IsReadOnly;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
                return false;
            Remove(item.Key);
            return true;
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
                throw new ArgumentNullException("item.Key");
            if (!TryGetValue(item.Key, out var value))
                return false;
            return Object.Equals(value, item);
        }
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Rank > 1 || arrayIndex >= array.Length || array.Length - arrayIndex < Count)
                throw new ArgumentException($"{arrayIndex} out of bound.", nameof(array));

            int index = arrayIndex;
            foreach (DictionaryEntry entry in _dictionary)
            {
                array[index] = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value!);
                index++;
            }
        }
        #endregion

    }
#else
    public class OrderedDictionary<TKey, TValue> : System.Collections.Generic.OrderedDictionary<TKey, TValue>
        where TKey : notnull
    {
        public OrderedDictionary()
            : base() { }
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
            : base(dictionary) { }
    }
#endif

}

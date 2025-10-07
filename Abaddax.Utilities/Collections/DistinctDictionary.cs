using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Abaddax.Utilities.Collections
{
    #region DebugView

    sealed class DistinctDictionary_DebugView<TKey, TValue>
        where TKey : notnull
    {
        private readonly DistinctDictionary<TKey, TValue> _dict;
        public DistinctDictionary_DebugView(DistinctDictionary<TKey, TValue> dictionary)
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
    /// Dictionary that contains unique keys and unique values
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(DistinctDictionary_DebugView<,>))]
    public class DistinctDictionary<TKey, TValue> :
        IEnumerable,
        IEnumerable<KeyValuePair<TKey, TValue>>,
        IReadOnlyCollection<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>,
        IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        private readonly HashSet<TValue> _values = new HashSet<TValue>();

        public DistinctDictionary()
        {

        }
        public DistinctDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
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
            get => _dictionary[key];
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                if (!_dictionary.ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    var currentValue = _dictionary[key];
                    try
                    {
                        _values.Remove(currentValue);
                        if (!_values.Add(value))
                            throw new ArgumentException("Value already exists");
                        _dictionary[key] = value;
                    }
                    catch (Exception ex)
                    {
                        UpdateValues();
                        throw;
                    }
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (_dictionary.ContainsKey(key))
                throw new ArgumentException("key already exists");
            if (!_values.Add(value))
                throw new ArithmeticException("value already exists");
            _dictionary.Add(key, value);
        }
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null)
                return false;
            if (_dictionary.ContainsKey(key))
                return false;
            if (!_values.Add(value))
                return false;
            _dictionary.Add(key, value);
            return true;
        }
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!_dictionary.Remove(key, out var value))
                return false;
            _values.Remove(value);
            return true;
        }
        public bool Remove(TValue value, IEqualityComparer<TValue>? comparer = null)
        {
            if (!TryGetKey(value, out var key, comparer))
                return false;
            return Remove(key);
        }

        public void Clear()
        {
            _dictionary.Clear();
            _values.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return _dictionary.ContainsKey(key);
        }
        public bool ContainsValue(TValue value)
        {
            return _values.Contains(value);
        }
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return _dictionary.TryGetValue(key, out value);
        }
        public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key, IEqualityComparer<TValue>? comparer = null)
        {
            comparer ??= EqualityComparer<TValue>.Default;
            key = default;
            if (!_values.Contains(value))
                return false;
            var entry = _dictionary.First(x => comparer.Equals(x.Value, value));
            key = entry.Key;
            return true;
        }

        #region IDictionary<TKey, TValue>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
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
        ICollection<TValue> IDictionary<TKey, TValue>.Values
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
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dictionary.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dictionary.Values;
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
        #endregion

        #region IEnumerable<KeyValuePair<TKey, TValue>>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).IsReadOnly;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
                return false;
            Remove(item.Key);
            return true;
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        #endregion

        #region Helper
        private void UpdateValues()
        {
            _values.Clear();
            foreach (var v in _dictionary.Values)
            {
                if (!_values.Add(v))
                    throw new Exception("Collection corrupted");
            }
        }
        #endregion
    }
}

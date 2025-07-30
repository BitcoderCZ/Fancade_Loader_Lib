using System.Collections;
using System.Diagnostics.CodeAnalysis;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Collections;

/// <summary>
/// Represents a dictionary with multiple values per key.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public sealed class MultiValueDictionary<TKey, TValue> : IDictionary<TKey, List<TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _dict;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    public MultiValueDictionary()
    {
        _dict = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    public MultiValueDictionary(int capacity)
    {
        _dict = new(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="MultiValueDictionary{TKey, TValue}"/>.</param>
    public MultiValueDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection)
    {
        _dict = new(collection);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="MultiValueDictionary{TKey, TValue}"/>.</param>
    public MultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
    {
        _dict = new(dictionary);
    }

    /// <inheritdoc/>
    public ICollection<TKey> Keys => _dict.Keys;

    /// <inheritdoc/>
    public ICollection<List<TValue>> Values => _dict.Values;

    /// <inheritdoc/>
    public int Count => _dict.Count;

    bool ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly => false;

    /// <inheritdoc/>
    public List<TValue> this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    /// <summary>
    /// Adds an element with the provided key and value to the <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The object to use as the key of the element to add.</param>
    /// <param name="value">The object to use as the value of the element to add.</param>
    public void Add(TKey key, TValue value)
        => GetOrAddValue(key).Add(value);

    /// <inheritdoc/>
    public void Add(TKey key, List<TValue> value)
        => _dict.Add(key, value);

    /// <summary>
    /// Adds multiple values to the provided key.
    /// </summary>
    /// <param name="key">The object to use as the key.</param>
    /// <param name="value">The values to add.</param>
    public void AddRange(TKey key, IEnumerable<TValue> value)
        => GetOrAddValue(key).AddRange(value);

    /// <inheritdoc/>
    public void Clear()
        => _dict.Clear();

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(TKey key)
    => _dict.Remove(key);

    /// <inheritdoc/>
#if NETSTANDARD
    public bool TryGetValue(TKey key, out List<TValue> value)
#else
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out List<TValue> value)
#endif
        => _dict.TryGetValue(key, out value);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
        => _dict.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

    /// <summary>
    /// Gets the values corresponding to the specified key.
    /// </summary>
    /// <remarks>
    /// If <paramref name="key"/> is not associated with any values, a new list is created and added to the <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </remarks>
    /// <param name="key">The key of the values to get.</param>
    /// <returns>The values associated with <paramref name="key"/>.</returns>
    public List<TValue> GetOrAddValue(TKey key)
    {
        if (!_dict.TryGetValue(key, out List<TValue>? list))
        {
            list = [];
            _dict.Add(key, list);
        }

        return list;
    }

    /// <inheritdoc/>
    void ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Add(item);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, List<TValue>>>.Contains(KeyValuePair<TKey, List<TValue>> item)
        => _dict.Contains(item);

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Remove(item);

    /// <inheritdoc/>
    void ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).CopyTo(array, arrayIndex);
}

/// <summary>
/// Utils for <see cref="MultiValueDictionary{TKey, TValue}"/>.
/// </summary>
#pragma warning disable SA1204 // Static elements should appear before instance elements
public static class MultiValueDictionaryUtils
#pragma warning restore SA1204 // Static elements should appear before instance elements
{
    /// <summary>
    /// Creates a <see cref="MultiValueDictionary{TKey, TValue}"/> from an <see cref="IEnumerable{T}"/> according to specified key selector and element selector functions.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to create a <see cref="MultiValueDictionary{TKey, TValue}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <returns>A <see cref="MultiValueDictionary{TKey, TValue}"/> that contains keys and values. The values within each group are in the same order as in source.</returns>
    public static MultiValueDictionary<TKey, TSource> ToMultiValueDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        MultiValueDictionary<TKey, TSource> dict = source.TryGetNonEnumeratedCount(out int collectionCount)
            ? new MultiValueDictionary<TKey, TSource>(collectionCount)
            : [];

        foreach (var item in source)
        {
            dict.Add(keySelector(item), item);
        }

        return dict;
    }

    /// <summary>
    /// Creates a <see cref="MultiValueDictionary{TKey, TValue}"/> from an <see cref="IEnumerable{T}"/> according to specified key selector and element selector functions.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to create a <see cref="MultiValueDictionary{TKey, TValue}"/> from.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
    /// <returns>A <see cref="MultiValueDictionary{TKey, TValue}"/> that contains values of type TElement selected from the input sequence.</returns>
    public static MultiValueDictionary<TKey, TElement> ToMultiValueDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        MultiValueDictionary<TKey, TElement> dict = source.TryGetNonEnumeratedCount(out int collectionCount)
            ? new MultiValueDictionary<TKey, TElement>(collectionCount)
            : [];

        foreach (var item in source)
        {
            dict.Add(keySelector(item), elementSelector(item));
        }

        return dict;
    }

#if NETSTANDARD
    /// <summary>
    ///   Attempts to determine the number of elements in a sequence without forcing an enumeration.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">A sequence that contains elements to be counted.</param>
    /// <param name="count">
    ///     When this method returns, contains the count of <paramref name="source" /> if successful,
    ///     or zero if the method failed to determine the count.</param>
    /// <returns>
    ///   <see langword="true" /> if the count of <paramref name="source"/> can be determined without enumeration;
    ///   otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///   The method performs a series of type tests, identifying common subtypes whose
    ///   count can be determined without enumerating; this includes <see cref="ICollection{T}"/>,
    ///   <see cref="ICollection"/> as well as internal types used in the LINQ implementation.
    ///
    ///   The method is typically a constant-time operation, but ultimately this depends on the complexity
    ///   characteristics of the underlying collection implementation.
    /// </remarks>
    private static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
    {
        ThrowIfNull(source, nameof(source));

        if (source is ICollection<TSource> collectionoft)
        {
            count = collectionoft.Count;
            return true;
        }

        if (source is ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        count = 0;
        return false;
    }
#endif
}

// <copyright file="MultiValueDictionary.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FancadeLoaderLib.Editing.Utils;

public sealed class MultiValueDictionary<TKey, TValue> : IDictionary<TKey, List<TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _dict;

    public MultiValueDictionary()
    {
        _dict = [];
    }

    public MultiValueDictionary(int capacity)
    {
        _dict = new(capacity);
    }

    public MultiValueDictionary(IEnumerable<KeyValuePair<TKey, List<TValue>>> collection)
    {
#pragma warning disable IDE0306 // Simplify collection initialization
        _dict = new(collection);
#pragma warning restore IDE0306
    }

    public MultiValueDictionary(IDictionary<TKey, List<TValue>> dictionary)
    {
        _dict = new(dictionary);
    }

    public ICollection<TKey> Keys => _dict.Keys;

    public ICollection<List<TValue>> Values => _dict.Values;

    public int Count => _dict.Count;

    bool ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly => false;

    public List<TValue> this[TKey key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public void Add(TKey key, TValue value)
        => GetValue(key).Add(value);

    public void Add(TKey key, List<TValue> value)
        => _dict.Add(key, value);

    public void AddRange(TKey key, IEnumerable<TValue> value)
        => GetValue(key).AddRange(value);

    public void Clear()
        => _dict.Clear();

    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    public bool Remove(TKey key)
    => _dict.Remove(key);

#if NET5_0_OR_GREATER
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out List<TValue> value)
#else
    public bool TryGetValue(TKey key, out List<TValue> value)
#endif
        => _dict.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dict.GetEnumerator();

#pragma warning disable CA1002 // Do not expose generic lists
    public List<TValue> GetValue(TKey key)
#pragma warning restore CA1002 // Do not expose generic lists
    {
        if (!_dict.TryGetValue(key, out List<TValue>? list))
        {
            list = [];
            _dict.Add(key, list);
        }

        return list;
    }

    void ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Add(item);

    public bool Contains(KeyValuePair<TKey, List<TValue>> item)
        => _dict.Contains(item);

    bool ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).Remove(item);

    void ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, List<TValue>>>)_dict).CopyTo(array, arrayIndex);
}

#pragma warning disable SA1204 // Static elements should appear before instance elements
public static class MultiValueDictionaryUtils
#pragma warning restore SA1204
{
    public static MultiValueDictionary<TKey, TValue> ToMultiValueDictionary<T, TKey, TValue>(this IEnumerable<T> collection, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        if (collection is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(collection));
        }

        if (keySelector is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(keySelector));
        }

        if (valueSelector is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(valueSelector));
        }

        MultiValueDictionary<TKey, TValue> dict =
#if NET6_0_OR_GREATER
            collection.TryGetNonEnumeratedCount(out int collectionCount)
                ? new MultiValueDictionary<TKey, TValue>(collectionCount)
                : [];
#else
            [];
#endif

        foreach (var item in collection)
        {
            dict.Add(keySelector(item), valueSelector(item));
        }

        return dict;
    }
}
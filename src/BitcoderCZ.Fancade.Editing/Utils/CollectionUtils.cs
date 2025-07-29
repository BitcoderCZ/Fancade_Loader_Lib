// <copyright file="CollectionUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class CollectionUtils
{
    public static TValue AddIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    {
        if (!dict.TryGetValue(key, out TValue? val))
        {
            val = defaultValue;
            dict.Add(key, val);
        }

        return val;
    }
}

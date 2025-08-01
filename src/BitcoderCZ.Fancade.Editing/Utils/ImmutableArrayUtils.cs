﻿// <copyright file="ImmutableArrayUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class ImmutableArrayUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(this ImmutableArray<T> array, Index index)
        => array[index.GetOffset(array.Length)];
}

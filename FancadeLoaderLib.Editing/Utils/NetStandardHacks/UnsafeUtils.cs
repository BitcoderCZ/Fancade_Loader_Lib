// <copyright file="UnsafeUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Editing.Utils.NetStandardHacks;

internal static class UnsafeUtils
{
    /// <summary>
    /// Reinterprets the given value of type <typeparamref name="TFrom" /> as a value of type <typeparamref name="TTo" />.
    /// </summary>
    /// <exception cref="NotSupportedException">The sizes of <typeparamref name="TFrom" /> and <typeparamref name="TTo" /> are not the same
    /// or the type parameters are not <see langword="struct"/>s.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TTo BitCast<TFrom, TTo>(TFrom source)
        where TFrom : unmanaged
        where TTo : unmanaged
        => sizeof(TFrom) == sizeof(TTo)
            ? Unsafe.ReadUnaligned<TTo>(ref Unsafe.As<TFrom, byte>(ref source))
            : throw new InvalidOperationException();
}

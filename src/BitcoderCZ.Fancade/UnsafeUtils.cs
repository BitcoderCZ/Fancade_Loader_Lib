// <copyright file="UnsafeUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

internal static class UnsafeUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TTo BitCast<TFrom, TTo>(TFrom source)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
#if NET5_0_OR_GREATER
#pragma warning disable IDE0022
        return Unsafe.BitCast<TFrom, TTo>(source);
#pragma warning restore IDE0022
#else
#pragma warning disable IDE0046 // Convert to conditional expression
        if (sizeof(TFrom) != sizeof(TTo))
        {
            ThrowNotSupportedException();
        }
#pragma warning restore IDE0046

        return Unsafe.ReadUnaligned<TTo>(ref Unsafe.As<TFrom, byte>(ref source));
#endif
    }
}

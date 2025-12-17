// <copyright file="SpanUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class SpanUtils
{
    public static bool AnyTrue(this Span<bool> span)
        => span.IndexOf(true) != -1;

    public static bool AnyTrue(this ReadOnlySpan<bool> span)
        => span.IndexOf(true) != -1;
}

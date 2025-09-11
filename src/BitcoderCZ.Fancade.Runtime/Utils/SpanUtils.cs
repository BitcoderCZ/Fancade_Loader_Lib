using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class SpanUtils
{
    public static bool AnyTrue(this Span<bool> span)
        => span.IndexOf(true) != -1;
    
    public static bool AnyTrue(this ReadOnlySpan<bool> span)
        => span.IndexOf(true) != -1;
}

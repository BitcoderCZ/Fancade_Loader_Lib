using BitcoderCZ.Fancade.Runtime.Exceptions;
using MathUtils.Vectors;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidTerminalException(string terminalName)
        => throw new InvalidTerminalException(terminalName);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidTerminalException(byte3 terminalPosition)
        => throw new InvalidTerminalException(terminalPosition);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowTimeoutException()
        => throw new TimeoutException();
}

using FancadeLoaderLib.Runtime.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Runtime.Utils;

internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidTerminalException(string terminalName)
        => throw new InvalidTerminalException(terminalName);
}

using System.Runtime.CompilerServices;

namespace FancadeLoaderLib.Common;

public static class Maths
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DivCeiling(int a, int b)
        => (a - 1) / b + 1;
}

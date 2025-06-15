using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FancadeLoaderLib.Common;

public static class Maths
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DivCeiling(int a, int b)
        => (a - 1) / b + 1;
}

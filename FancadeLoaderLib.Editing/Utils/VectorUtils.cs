using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Editing.Utils;

internal static class VectorUtils
{
    public static (int3 Smaller, int3 Larger) MinMax(this int3 a, int3 b)
        => (int3.Min(a, b), int3.Max(a, b));
}

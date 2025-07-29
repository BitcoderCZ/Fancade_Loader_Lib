using MathUtils.Vectors;
using System.Collections.Generic;

namespace BitcoderCZ.Fancade.Utils;

internal sealed class PositionComparer : IComparer<int3>
{
    public static readonly PositionComparer Instance = new();

    private PositionComparer()
    {
    }

    public int Compare(int3 x, int3 y)
    {
        int cmpZ = x.Z.CompareTo(y.Z);
        if (cmpZ != 0)
        {
            return cmpZ;
        }

        int cmpY = x.Y.CompareTo(y.Y);
        return cmpY != 0 ? cmpY : x.X.CompareTo(y.X);
    }
}

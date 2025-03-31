using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib.Tests.Common;

public sealed class PrefabSegmentPositionComparer : IComparer<PrefabSegment>
{
    public static readonly PrefabSegmentPositionComparer Instance = new();

    private PrefabSegmentPositionComparer()
    {
    }

    public int Compare(PrefabSegment? x, PrefabSegment? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }
        else if (x is null)
        {
            return -1;
        }
        else if (y is null)
        {
            return 1;
        }

        int3 xPos = x.PosInPrefab;
        int3 yPos = y.PosInPrefab;

        int cmpZ = xPos.Z.CompareTo(yPos.Z);
        if (cmpZ != 0)
        {
            return cmpZ;
        }

        int cmpY = xPos.Y.CompareTo(yPos.Y);
        return cmpY != 0 ? cmpY : xPos.X.CompareTo(yPos.X);
    }
}

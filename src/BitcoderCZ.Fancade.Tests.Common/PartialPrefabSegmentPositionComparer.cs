using BitcoderCZ.Fancade.Partial;
using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Tests.Common;

public sealed class PartialPrefabSegmentPositionComparer : IComparer<PartialPrefabSegment>
{
    public static readonly PartialPrefabSegmentPositionComparer Instance = new();

    private PartialPrefabSegmentPositionComparer()
    {
    }

    public int Compare(PartialPrefabSegment? x, PartialPrefabSegment? y)
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

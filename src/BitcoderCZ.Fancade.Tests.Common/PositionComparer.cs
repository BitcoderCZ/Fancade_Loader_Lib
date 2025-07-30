using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Tests.Common;

public sealed class PositionComparer : IComparer<ushort3>
{
    public static readonly PositionComparer Instance = new();

    private PositionComparer()
    {
    }

    public int Compare(ushort3 x, ushort3 y)
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

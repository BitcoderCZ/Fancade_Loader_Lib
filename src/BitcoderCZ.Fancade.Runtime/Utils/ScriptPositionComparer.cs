using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Runtime.Utils;

internal sealed class ScriptPositionComparer : IComparer<int3>
{
    public static readonly ScriptPositionComparer Instance = new();

    private ScriptPositionComparer()
    {
    }

    public int Compare(int3 x, int3 y)
    {
        int cmpZ = y.Z.CompareTo(x.Z);
        if (cmpZ != 0)
        {
            return cmpZ;
        }

        int cmpY = y.Y.CompareTo(x.Y);
        return cmpY != 0 ? cmpY : x.X.CompareTo(y.X);
    }
}
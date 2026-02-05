using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BitcoderCZ.Fancade.Tests.Common;

public sealed class BlockDataComparer : IEqualityComparer<ArrayBlockData>
{
    public static readonly BlockDataComparer Instance = new();

    private BlockDataComparer()
    {
    }

    public bool Equals(ArrayBlockData? x, ArrayBlockData? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }

        return x.Size == y.Size &&
            SequenceEquals(x, y);
    }

    public int GetHashCode([DisallowNull] ArrayBlockData obj)
        => throw new InvalidOperationException();

    private static bool SequenceEquals(ArrayBlockData a, ArrayBlockData b)
    {
        Debug.Assert(a.Size == b.Size);

        for (int z = 0; z < a.Size.Z; z++)
        {
            for (int y = 0; y < a.Size.Y; y++)
            {
                for (int x = 0; x < a.Size.X; x++)
                {
                    if (a.GetBlockUnchecked(new int3(x, y, z)) != b.GetBlockUnchecked(new int3(x, y, z)))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}

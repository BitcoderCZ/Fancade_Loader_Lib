using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class BlockDataComparer : IEqualityComparer<BlockData>
{
    public bool Equals(BlockData? x, BlockData? y)
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

    public int GetHashCode([DisallowNull] BlockData obj)
        => throw new InvalidOperationException();

    private static bool SequenceEquals(BlockData a, BlockData b)
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

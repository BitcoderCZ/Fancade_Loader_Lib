﻿using BitcoderCZ.Fancade.Partial;
using System.Diagnostics.CodeAnalysis;

namespace BitcoderCZ.Fancade.Tests.Common;

public sealed class PartialPrefabSegmentComparer : IEqualityComparer<PartialPrefabSegment>
{
    public static readonly PartialPrefabSegmentComparer Instance = new();

    private PartialPrefabSegmentComparer()
    {
    }

    public bool Equals(PartialPrefabSegment? x, PartialPrefabSegment? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }

        return x.PrefabId == y.PrefabId &&
            x.PosInPrefab == y.PosInPrefab;
    }

    public int GetHashCode([DisallowNull] PartialPrefabSegment obj)
        => throw new InvalidOperationException();
}

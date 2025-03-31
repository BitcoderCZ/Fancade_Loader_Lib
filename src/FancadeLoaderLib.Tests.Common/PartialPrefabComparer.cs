using FancadeLoaderLib.Partial;
using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests.Common;

public sealed class PartialPrefabComparer : IEqualityComparer<PartialPrefab>
{
    public static readonly PartialPrefabComparer Instance = new();

    private PartialPrefabComparer()
    {
    }

    private readonly PartialPrefabSegmentComparer _prefabComparer = PartialPrefabSegmentComparer.Instance;

    public bool Equals(PartialPrefab? x, PartialPrefab? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        else if (x is null || y is null)
        {
            return false;
        }

        return x.Id == y.Id &&
            x.Count == y.Count &&
            x.Name == y.Name &&
            x.Type == y.Type &&
            x.OrderedKeys.SequenceEqual(y.OrderedKeys) &&
            x.OrderedValues.SequenceEqual(y.OrderedValues, _prefabComparer);
    }

    public int GetHashCode([DisallowNull] PartialPrefab obj)
        => throw new InvalidOperationException();
}

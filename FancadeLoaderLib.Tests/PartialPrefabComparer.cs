using FancadeLoaderLib.Partial;
using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PartialPrefabComparer : IEqualityComparer<PartialPrefab>
{
    private readonly PartialPrefabSegmentComparer _prefabComparer = new();

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
            x.Keys.SequenceEqual(y.Keys) &&
            x.Values.SequenceEqual(y.Values, _prefabComparer);
    }

    public int GetHashCode([DisallowNull] PartialPrefab obj)
        => throw new InvalidOperationException();
}

using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PrefabSegmentComparer : IEqualityComparer<PrefabSegment>
{
	public bool Equals(PrefabSegment? x, PrefabSegment? y)
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
			x.PosInPrefab == y.PosInPrefab &&
			EqualsVoxels(x.Voxels, y.Voxels);
	}

	public int GetHashCode([DisallowNull] PrefabSegment obj)
		=> throw new InvalidOperationException();

	private static bool EqualsVoxels(Voxel[]? a, Voxel[]? b)
		=> ReferenceEquals(a, b) || (a is not null && b is not null && a.SequenceEqual(b));
}

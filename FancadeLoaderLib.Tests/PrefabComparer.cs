using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PrefabComparer : IEqualityComparer<Prefab>
{
	public bool Equals(Prefab? x, Prefab? y)
	{
		if (ReferenceEquals(x, y))
		{
			return true;
		}
		else if (x is null || y is null)
		{
			return false;
		}

		return x.GroupId == y.GroupId &&
			x.PosInGroup == y.PosInGroup &&
			EqualsVoxels(x.Voxels, y.Voxels);
	}

	public int GetHashCode([DisallowNull] Prefab obj)
		=> throw new InvalidOperationException();

	private static bool EqualsVoxels(Voxel[]? a, Voxel[]? b)
		=> ReferenceEquals(a, b) || (a is not null && b is not null && a.SequenceEqual(b));
}

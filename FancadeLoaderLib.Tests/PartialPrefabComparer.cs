using FancadeLoaderLib.Partial;
using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PartialPrefabComparer : IEqualityComparer<PartialPrefab>
{
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

		return x.GroupId == y.GroupId &&
			x.PosInGroup == y.PosInGroup;
	}

	public int GetHashCode([DisallowNull] PartialPrefab obj)
		=> throw new InvalidOperationException();
}

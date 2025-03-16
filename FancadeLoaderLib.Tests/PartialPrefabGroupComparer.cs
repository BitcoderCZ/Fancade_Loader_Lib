using FancadeLoaderLib.Partial;
using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PartialPrefabGroupComparer : IEqualityComparer<PartialPrefabGroup>
{
	private readonly PartialPrefabComparer _prefabComparer = new();

	public bool Equals(PartialPrefabGroup? x, PartialPrefabGroup? y)
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

	public int GetHashCode([DisallowNull] PartialPrefabGroup obj)
		=> throw new InvalidOperationException();
}

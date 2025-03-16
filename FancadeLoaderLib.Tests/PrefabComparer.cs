using System.Diagnostics.CodeAnalysis;

namespace FancadeLoaderLib.Tests;

internal sealed class PrefabComparer : IEqualityComparer<Prefab>
{
	private readonly BlockDataComparer _blockDataComparer = new();
	private readonly PrefabSegmentComparer _prefabComparer = new();

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

		return x.Id == y.Id &&
			x.Count == y.Count &&
			x.Name == y.Name &&
			x.Collider == y.Collider &&
			x.Type == y.Type &&
			x.BackgroundColor == y.BackgroundColor &&
			x.Editable == y.Editable &&
			x.Settings.SequenceEqual(y.Settings) &&
			x.Connections.SequenceEqual(y.Connections) &&
			x.Keys.SequenceEqual(y.Keys) &&
			x.Values.SequenceEqual(y.Values, _prefabComparer) &&
			_blockDataComparer.Equals(x.Blocks, y.Blocks);
	}

	public int GetHashCode([DisallowNull] Prefab obj)
		=> throw new InvalidOperationException();
}

using System.Diagnostics.CodeAnalysis;

namespace BitcoderCZ.Fancade.Tests.Common;

public sealed class PrefabComparer : IEqualityComparer<Prefab>
{
    public static readonly PrefabComparer Instance = new();

    private PrefabComparer()
    {
    }

    private readonly BlockDataComparer _blockDataComparer = BlockDataComparer.Instance;
    private readonly PrefabSegmentComparer _prefabComparer = PrefabSegmentComparer.Instance;

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

        var a = x.Settings.OrderBy(item => item.Key, PositionComparer.Instance).Select(item => item.Value);
        var b = y.Settings.OrderBy(item => item.Key, PositionComparer.Instance).Select(item => item.Value);

        return x.Id == y.Id &&
            x.Count == y.Count &&
            x.Name == y.Name &&
            x.Collider == y.Collider &&
            x.Type == y.Type &&
            x.BackgroundColor == y.BackgroundColor &&
            x.Editable == y.Editable &&
            x.Settings.OrderBy(item => item.Key, PositionComparer.Instance).Select(item => item.Value).SequenceEqual(y.Settings.OrderBy(item => item.Key, PositionComparer.Instance).Select(item => item.Value), SettingsComparer.Instance) &&
            x.Connections.SequenceEqual(y.Connections) &&
            x.OrderedKeys.SequenceEqual(y.OrderedKeys) &&
            x.OrderedValues.SequenceEqual(y.OrderedValues, _prefabComparer) &&
            _blockDataComparer.Equals(x.Blocks, y.Blocks);
    }

    public int GetHashCode([DisallowNull] Prefab obj)
        => throw new InvalidOperationException();

    public sealed class SettingsComparer : IEqualityComparer<List<PrefabSetting>>
    {
        public static readonly SettingsComparer Instance = new();

        private SettingsComparer()
        {
        }

        public bool Equals(List<PrefabSetting>? x, List<PrefabSetting>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            else if (x is null || y is null)
            {
                return false;
            }

            return x.SequenceEqual(y);
        }

        public int GetHashCode([DisallowNull] List<PrefabSetting> obj)
            => throw new InvalidOperationException();
    }
}

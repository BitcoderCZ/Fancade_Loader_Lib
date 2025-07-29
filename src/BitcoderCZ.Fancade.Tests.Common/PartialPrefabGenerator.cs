using BitcoderCZ.Fancade;
using BitcoderCZ.Fancade.Partial;
using MathUtils.Vectors;
using System.Diagnostics;

namespace BitcoderCZ.Fancade.Tests.Common;

public static class PartialPrefabGenerator
{
    public static PartialPrefab CreatePrefab(ushort id, IEnumerable<PartialPrefabSegment> segments, string? name = null)
        => new PartialPrefab(id, name ?? $"Prefab {id}", PrefabType.Normal, segments);

    public static PartialPrefab CreatePrefab(ushort id, int segmentCount, string? name = null)
        => CreatePrefab(id, CreateSegments(id, segmentCount), name);

    public static PartialPrefab CreatePrefab(ushort id, int3[] posititons, string? name = null)
        => CreatePrefab(id, CreateSegments(id, posititons), name);

    public static IEnumerable<PartialPrefabSegment> CreateSegments(ushort id, int count)
    {
        Debug.Assert(count < 4 * 4 * 4);

        // Generate in reverse to catch potential bugs caused by segments not being ordered
        for (int i = count - 1; i >= 0; i--)
        {
            int x = i % 4;
            int y = (i / 4) % 4;
            int z = i / (4 * 4);

            yield return new PartialPrefabSegment(id, new int3(x, y, z));
        }
    }

    public static IEnumerable<PartialPrefabSegment> CreateSegments(ushort id, int3[] positions)
    {
        Debug.Assert(positions.Length < 4 * 4 * 4);

        // Generate in reverse to catch potential bugs caused by segments not being ordered
        for (int i = positions.Length - 1; i >= 0; i--)
        {
            int3 pos = positions[i];
            yield return new PartialPrefabSegment(id, pos);
        }
    }
}

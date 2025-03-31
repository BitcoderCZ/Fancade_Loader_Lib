using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Tests.Common;

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

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PartialPrefabSegment(id, new int3(x, y, z));
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }

    public static IEnumerable<PartialPrefabSegment> CreateSegments(ushort id, int3[] posititons)
    {
        Debug.Assert(posititons.Length < 4 * 4 * 4);

        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (posititons.Contains(new int3(x, y, z)))
                    {
                        yield return new PartialPrefabSegment(id, new int3(x, y, z));
                    }
                }
            }
        }
    }
}

using MathUtils.Vectors;
using System.Diagnostics;

namespace FancadeLoaderLib.Tests.Common;

public static class PrefabGenerator
{
    public static Prefab CreatePrefab(ushort id, IEnumerable<PrefabSegment> segments, string? name = null)
        => new Prefab(id, name ?? $"Prefab {id}", PrefabCollider.Box, PrefabType.Normal, FcColorUtils.DefaultBackgroundColor, true, null, null, null, segments);

    public static Prefab CreatePrefab(ushort id, int segmentCount, string? name = null, bool initVoxels = false)
        => CreatePrefab(id, CreateSegments(id, segmentCount, initVoxels), name);

    public static Prefab CreatePrefab(ushort id, int3[] posititons, string? name = null, bool initVoxels = false)
        => CreatePrefab(id, CreateSegments(id, posititons, initVoxels), name);

    public static IEnumerable<PrefabSegment> CreateSegments(ushort id, int count, bool initVoxels = false)
    {
        Debug.Assert(count < 4 * 4 * 4);

        int c = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    yield return new PrefabSegment(id, new int3(x, y, z), initVoxels ? new Voxel[8 * 8 * 8] : null);
                    if (++c >= count)
                    {
                        yield break;
                    }
                }
            }
        }
    }

    public static IEnumerable<PrefabSegment> CreateSegments(ushort id, int3[] posititons, bool initVoxels = false)
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
                        yield return new PrefabSegment(id, new int3(x, y, z), initVoxels ? new Voxel[8 * 8 * 8] : null);
                    }
                }
            }
        }
    }
}

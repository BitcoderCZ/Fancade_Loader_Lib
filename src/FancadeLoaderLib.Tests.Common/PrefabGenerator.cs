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

        // Generate in reverse to catch potential bugs caused by segments not being ordered
        for (int i = count - 1; i >= 0; i--)
        {
            int x = i % 4;
            int y = (i / 4) % 4;
            int z = i / (4 * 4);

            yield return new PrefabSegment(id, new int3(x, y, z), initVoxels ? new Voxel[8 * 8 * 8] : null);
        }
    }

    public static IEnumerable<PrefabSegment> CreateSegments(ushort id, int3[] positions, bool initVoxels = false)
    {
        Debug.Assert(positions.Length < 4 * 4 * 4);

        // Generate in reverse to catch potential bugs caused by segments not being ordered
        for (int i = positions.Length - 1; i >= 0; i--)
        {
            int3 pos = positions[i];
            yield return new PrefabSegment(id, pos, initVoxels ? new Voxel[8 * 8 * 8] : null);
        }
    }
}

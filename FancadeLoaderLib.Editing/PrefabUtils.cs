using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing;

public static class PrefabUtils
{
    public static bool Fill(this Prefab prefab, PrefabList prefabList, int3 fromVoxel, int3 toVoxel, Voxel value, bool overwriteVoxels, bool overwriteBlocks,  BlockInstancesCache? cache)
    {
        if (cache is not null && cache.BLockId != prefab.Id)
        {
            ThrowArgumentException($"{nameof(cache)}.{nameof(BlockInstancesCache.BLockId)} must be equal to {nameof(prefab)}.{nameof(Prefab.Id)}.", nameof(cache));
        }

        (fromVoxel, toVoxel) = VectorUtils.MinMax(ClampVoxelToPrefab(fromVoxel), ClampVoxelToPrefab(toVoxel));

        int3 fromSegment = VoxelToSegment(fromVoxel);
        int3 toSegment = VoxelToSegment(toVoxel);

        if (!value.IsEmpty)
        {
            if (!prefab.EnsureSegments(prefabList, fromSegment, toSegment, overwriteBlocks, cache))
            {
                return false;
            }
        }

        if (overwriteVoxels || value.IsEmpty)
        {
            for (int z = fromVoxel.Z; z <= toVoxel.Z; z++)
            {
                for (int y = fromVoxel.Y; y <= toVoxel.Y; y++)
                {
                    for (int x = fromVoxel.X; x <= toVoxel.X; x++)
                    {
                        if (prefab.TryGetValue(VoxelToSegment(new int3(x, y, z)), out var segment) && segment.Voxels is not null)
                        {
                            segment.Voxels[PrefabSegment.IndexVoxels(new int3(x, y, z) % 8)] = value;
                        }
                    }
                }
            }
        }
        else
        {
            for (int z = fromVoxel.Z; z <= toVoxel.Z; z++)
            {
                for (int y = fromVoxel.Y; y <= toVoxel.Y; y++)
                {
                    for (int x = fromVoxel.X; x <= toVoxel.X; x++)
                    {
                        if (prefab.TryGetValue(VoxelToSegment(new int3(x, y, z)), out var segment) && segment.Voxels is not null)
                        {
                            int index = PrefabSegment.IndexVoxels(new int3(x, y, z) % 8);
                            if (segment.Voxels[index].IsEmpty)
                            {
                                segment.Voxels[index] = value;
                            }
                        }
                    }
                }
            }
        }

        if (value.IsEmpty)
        {
            prefabList.RemoveEmptySegmentsFromPrefab(prefab.Id, cache);
        }

        return true;
    }

#if NETSTANDARD
    public static unsafe void FillColor(this Prefab prefab, int3 fromVoxel, int3 toVoxel, int sideIndex, FcColor color)
#else
    public static void FillColor(this Prefab prefab, int3 fromVoxel, int3 toVoxel, int sideIndex, FcColor color)
#endif
    {
        if (sideIndex < 0 || sideIndex > 5)
        {
            ThrowArgumentOutOfRangeException(nameof(sideIndex), $"{nameof(sideIndex)} must be between 0 and 5.");
        }

        (fromVoxel, toVoxel) = VectorUtils.MinMax(ClampVoxelToPrefab(fromVoxel), ClampVoxelToPrefab(toVoxel));

        byte colorByte = (byte)color;

        for (int z = fromVoxel.Z; z <= toVoxel.Z; z++)
        {
            for (int y = fromVoxel.Y; y <= toVoxel.Y; y++)
            {
                for (int x = fromVoxel.X; x <= toVoxel.X; x++)
                {
                    if (prefab.TryGetValue(VoxelToSegment(new int3(x, y, z)), out var segment) && segment.Voxels is not null)
                    {
                        int index = PrefabSegment.IndexVoxels(new int3(x, y, z) % 8);
                        if (segment.Voxels[index].IsEmpty)
                        {
                            segment.Voxels[index].Colors[sideIndex] = colorByte;
                        }
                    }
                }
            }
        }
    }

    public static bool EnsureSegments(this Prefab prefab, PrefabList prefabList, int3 from, int3 to, bool overwriteBlocks, BlockInstancesCache? cache)
    {
        from = ClampSegmentToPrefab(from);
        to = ClampSegmentToPrefab(to);

        for (int z = from.Z; z <= to.Z; z++)
        {
            for (int y = from.Y; y <= to.Y; y++)
            {
                for (int x = from.X; x <= to.X; x++)
                {
                    int3 pos = new int3(x, y, z);
                    if (!prefab.ContainsKey(pos))
                    {
                        prefabList.AddSegmentToPrefab(prefab.Id, new PrefabSegment(prefab.Id, pos), overwriteBlocks, cache);
                    }
                }
            }
        }

        return true;
    }

    public static int3 ClampSegmentToPrefab(int3 pos)
        => int3.Max(int3.Min(pos, new int3(Prefab.MaxSize, Prefab.MaxSize, Prefab.MaxSize) - 1), int3.Zero);

    public static int3 ClampVoxelToPrefab(int3 pos)
        => int3.Max(int3.Min(pos, new int3(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize)) - 1, int3.Zero);

    public static int3 ClampVoxelToSegment(int3 pos)
        => int3.Max(int3.Min(pos, new int3(7, 7, 7)), int3.Zero);

    public static int3 VoxelToSegment(int3 pos)
        => pos / 8;
}

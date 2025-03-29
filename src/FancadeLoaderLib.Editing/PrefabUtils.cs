using FancadeLoaderLib.Editing.Utils;
using FancadeLoaderLib.Exceptions;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Utils for working with <see cref="Prefab"/>.
/// </summary>
public static class PrefabUtils
{
    /// <summary>
    /// Fill a region of a prefab.
    /// </summary>
    /// <param name="prefab">The prefab to fill.</param>
    /// <param name="fromVoxel">The start position of the fill, inclusive.</param>
    /// <param name="toVoxel">The end position of the fill, inclusive.</param>
    /// <param name="value">The value to fill with.</param>
    /// <param name="overwriteVoxels">
    /// If <see langword="true"/>, non empty voxels will be overwritten,
    /// if <see langword="false"/>, only empty voxels will be written to.
    /// </param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten (if segments get added to the prefab),
    /// if <see langword="false"/>, if a added segment would be placed at a position that is already occupied, <see cref="BlockObstructedException"/> is thrown.
    /// </param>
    /// <param name="prefabList">A <see cref="PrefabList"/> that <paramref name="prefab"/> is in.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    public static void Fill(this Prefab prefab, int3 fromVoxel, int3 toVoxel, Voxel value, bool overwriteVoxels, bool overwriteBlocks, PrefabList? prefabList, BlockInstancesCache? cache)
    {
        (fromVoxel, toVoxel) = VectorUtils.MinMax(fromVoxel, toVoxel);

        if (!HasOverlap(fromVoxel, toVoxel, int3.Zero, (int3.One * 8 * Prefab.MaxSize) - 1))
        {
            return;
        }

        fromVoxel = ClampVoxelToPrefab(fromVoxel);
        toVoxel = ClampVoxelToPrefab(toVoxel);

        int3 fromSegment = VoxelToSegment(fromVoxel);
        int3 toSegment = VoxelToSegment(toVoxel);

        if (!value.IsEmpty)
        {
            prefab.EnsureSegmentVoxels(fromSegment, toSegment, overwriteBlocks, prefabList, cache);
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
            prefabList?.RemoveEmptySegmentsFromPrefab(prefab.Id, cache);
        }
    }

    /// <summary>
    /// Try to fill a region of a prefab.
    /// </summary>
    /// <param name="prefab">The prefab to fill.</param>
    /// <param name="fromVoxel">The start position of the fill, inclusive.</param>
    /// <param name="toVoxel">The end position of the fill, inclusive.</param>
    /// <param name="value">The value to fill with.</param>
    /// <param name="overwriteVoxels">
    /// If <see langword="true"/>, non empty voxels will be overwritten,
    /// if <see langword="false"/>, only empty voxels will be written to.
    /// </param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten (if segments get added to the prefab),
    /// if <see langword="false"/>, if a added segment would be placed at a position that is already occupied, <see langword="false"/> is returned.
    /// </param>
    /// <param name="prefabList">A <see cref="PrefabList"/> that <paramref name="prefab"/> is in.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if the fill was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryFill(this Prefab prefab, int3 fromVoxel, int3 toVoxel, Voxel value, bool overwriteVoxels, bool overwriteBlocks, PrefabList? prefabList, BlockInstancesCache? cache)
    {
        (fromVoxel, toVoxel) = VectorUtils.MinMax(fromVoxel, toVoxel);

        if (!HasOverlap(fromVoxel, toVoxel, int3.Zero, (int3.One * 8 * Prefab.MaxSize) - 1))
        {
            return true;
        }

        fromVoxel = ClampVoxelToPrefab(fromVoxel);
        toVoxel = ClampVoxelToPrefab(toVoxel);

        int3 fromSegment = VoxelToSegment(fromVoxel);
        int3 toSegment = VoxelToSegment(toVoxel);

        if (!value.IsEmpty)
        {
            if (!prefab.TryEnsureSegmentVoxels(fromSegment, toSegment, overwriteBlocks, prefabList, cache, out _))
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
            prefabList?.RemoveEmptySegmentsFromPrefab(prefab.Id, cache);
        }

        return true;
    }

    /// <summary>
    /// Sets the color of a side of voxels in a specified region.
    /// </summary>
    /// <param name="prefab">The prefab to fill.</param>
    /// <param name="fromVoxel">The start position of the fill, inclusive.</param>
    /// <param name="toVoxel">The end position of the fill, inclusive.</param>
    /// <param name="sideIndex">Index of the side to set the color.</param>
    /// <param name="color">The color to set.</param>
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

        (fromVoxel, toVoxel) = VectorUtils.MinMax(fromVoxel, toVoxel);

        if (!HasOverlap(fromVoxel, toVoxel, int3.Zero, (int3.One * 8 * Prefab.MaxSize) - 1))
        {
            return;
        }

        fromVoxel = ClampVoxelToPrefab(fromVoxel);
        toVoxel = ClampVoxelToPrefab(toVoxel);

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
                        if (!segment.Voxels[index].IsEmpty)
                        {
                            segment.Voxels[index].Colors[sideIndex] = colorByte;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Makes sure that <paramref name="prefab"/> contains segments with <see cref="PrefabSegment.Voxels"/> in a specified region.
    /// </summary>
    /// <param name="prefab">The prefab to add the segments to.</param>
    /// <param name="from">The start position of regiom, inclusive.</param>
    /// <param name="to">The end position of regiom, inclusive.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten (if segments get added to the prefab),
    /// if <see langword="false"/>, if a added segment would be placed at a position that is already occupied, <see cref="BlockObstructedException"/> is thrown.
    /// </param>
    /// <param name="prefabList">A <see cref="PrefabList"/> that <paramref name="prefab"/> is in.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns><see langword="true"/> if segments were added; otherwise, <see langword="false"/>.</returns>
    public static bool EnsureSegmentVoxels(this Prefab prefab, int3 from, int3 to, bool overwriteBlocks, PrefabList? prefabList, BlockInstancesCache? cache)
    {
        (from, to) = VectorUtils.MinMax(from, to);

        if (!HasOverlap(from, to, int3.Zero, (int3.One * Prefab.MaxSize) - 1))
        {
            return false;
        }

        from = ClampSegmentToPrefab(from);
        to = ClampSegmentToPrefab(to);

        bool added = false;

        for (int z = from.Z; z <= to.Z; z++)
        {
            for (int y = from.Y; y <= to.Y; y++)
            {
                for (int x = from.X; x <= to.X; x++)
                {
                    int3 pos = new int3(x, y, z);
                    if (prefab.TryGetValue(pos, out var segment))
                    {
                        if (segment.Voxels is null)
                        {
                            segment.Voxels = new Voxel[8 * 8 * 8];
                        }
                    }
                    else
                    {
                        added = true;

                        if (prefabList is not null)
                        {
                            prefabList.AddSegmentToPrefab(prefab.Id, new PrefabSegment(prefab.Id, pos, new Voxel[8 * 8 * 8]), overwriteBlocks, cache);
                        }
                        else
                        {
                            prefab.Add(new PrefabSegment(prefab.Id, pos, new Voxel[8 * 8 * 8]));
                        }
                    }
                }
            }
        }

        return added;
    }

    /// <summary>
    /// Makes sure that <paramref name="prefab"/> contains segments with <see cref="PrefabSegment.Voxels"/> in a specified region.
    /// </summary>
    /// <param name="prefab">The prefab to add the segments to.</param>
    /// <param name="from">The start position of regiom, inclusive.</param>
    /// <param name="to">The end position of regiom, inclusive.</param>
    /// <param name="overwriteBlocks">
    /// If <see langword="true"/>, blocks will be overwritten (if segments get added to the prefab),
    /// if <see langword="false"/>, if a added segment would be placed at a position that is already occupied, <see langword="false"/> is returned.
    /// </param>
    /// <param name="prefabList">A <see cref="PrefabList"/> that <paramref name="prefab"/> is in.</param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <param name="segmentsAdded"><see langword="true"/> if segments were added; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryEnsureSegmentVoxels(this Prefab prefab, int3 from, int3 to, bool overwriteBlocks, PrefabList? prefabList, BlockInstancesCache? cache, out bool segmentsAdded)
    {
        segmentsAdded = false;

        (from, to) = VectorUtils.MinMax(from, to);

        if (!HasOverlap(from, to, int3.Zero, (int3.One * Prefab.MaxSize) - 1))
        {
            return true;
        }

        from = ClampSegmentToPrefab(from);
        to = ClampSegmentToPrefab(to);

        List<int3>? segmentsToAdd = null;
        if (prefabList is not null)
        {
            segmentsToAdd = [];
        }

        for (int z = from.Z; z <= to.Z; z++)
        {
            for (int y = from.Y; y <= to.Y; y++)
            {
                for (int x = from.X; x <= to.X; x++)
                {
                    int3 pos = new int3(x, y, z);
                    if (prefab.TryGetValue(pos, out var segment))
                    {
                        if (segment.Voxels is null)
                        {
                            segment.Voxels = new Voxel[8 * 8 * 8];
                        }
                    }
                    else
                    {
                        segmentsAdded = true;

                        if (prefabList is not null)
                        {
                            Debug.Assert(segmentsToAdd is not null, "When prefabList is not null, segmentsToAdd should not be null.");
                            segmentsToAdd.Add(pos);
                        }
                        else
                        {
                            prefab.Add(new PrefabSegment(prefab.Id, pos, new Voxel[8 * 8 * 8]));
                        }
                    }
                }
            }
        }

        if (prefabList is not null)
        {
            Debug.Assert(segmentsToAdd is not null, "When prefabList is not null, segmentsToAdd should not be null.");
            foreach (var pos in segmentsToAdd)
            {
                if (!prefabList.CanAddSegmentToPrefab(prefab.Id, pos, overwriteBlocks, cache))
                {
                    return false;
                }
            }

            foreach (var pos in segmentsToAdd)
            {
                prefabList.AddSegmentToPrefab(prefab.Id, new PrefabSegment(prefab.Id, pos, new Voxel[8 * 8 * 8]), overwriteBlocks, cache);
            }
        }

        return true;
    }

    /// <summary>
    /// Clamps a segment position.
    /// </summary>
    /// <param name="pos">The position to clamp.</param>
    /// <returns>The clamped position.</returns>
    public static int3 ClampSegmentToPrefab(int3 pos)
        => int3.Max(int3.Min(pos, new int3(Prefab.MaxSize, Prefab.MaxSize, Prefab.MaxSize) - 1), int3.Zero);

    /// <summary>
    /// Clamps a voxel position to a prefab.
    /// </summary>
    /// <param name="pos">The position to clamp.</param>
    /// <returns>The clamped position.</returns>
    public static int3 ClampVoxelToPrefab(int3 pos)
        => int3.Max(int3.Min(pos, new int3(8 * Prefab.MaxSize, 8 * Prefab.MaxSize, 8 * Prefab.MaxSize) - 1), int3.Zero);

    /// <summary>
    /// Clamps a voxel position to a segment.
    /// </summary>
    /// <param name="pos">The position to clamp.</param>
    /// <returns>The clamped position.</returns>
    public static int3 ClampVoxelToSegment(int3 pos)
        => int3.Max(int3.Min(pos, new int3(7, 7, 7)), int3.Zero);

    /// <summary>
    /// Converts a voxel position to a segment position.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>The converted postition.</returns>
    public static int3 VoxelToSegment(int3 pos)
        => pos / 8;

    private static bool HasOverlap(int3 minA, int3 maxA, int3 minB, int3 maxB)
        => (minA.X <= maxB.X && maxA.X >= minB.X) &&
            (minA.Y <= maxB.Y && maxA.Y >= minB.Y) &&
            (minA.Z <= maxB.Z && maxA.Z >= minB.Z);
}

// <copyright file="PrefabListUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Partial;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FancadeLoaderLib.Editing;

/// <summary>
/// Utils for working with <see cref="PrefabList"/>.
/// </summary>
public static class PrefabListUtils
{
    /// <summary>
    /// Gets all of the levels from the prefab list.
    /// </summary>
    /// <remarks>
    /// <see cref="Prefab.Type"/> == <see cref="PrefabType.Level"/>.
    /// </remarks>
    /// <param name="list">The list to get the levels from.</param>
    /// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
    public static IEnumerable<Prefab> GetLevels(this PrefabList list)
        => list.Prefabs.Where(group => group.Type == PrefabType.Level);

    /// <summary>
    /// Gets all of the levels from the prefab list.
    /// </summary>
    /// <remarks>
    /// <see cref="Prefab.Type"/> == <see cref="PrefabType.Level"/>.
    /// </remarks>
    /// <param name="list">The list to get the levels from.</param>
    /// <returns><see cref="IEnumerable{T}"/> iterating over the levels in <paramref name="list"/>.</returns>
    public static IEnumerable<PartialPrefab> GetLevels(this PartialPrefabList list)
        => list.Prefabs.Where(group => group.Type == PrefabType.Level);

    /// <summary>
    /// Gets all of the blocks from the prefab list.
    /// </summary>
    /// <remarks>
    /// <see cref="Prefab.Type"/> != <see cref="PrefabType.Level"/>.
    /// </remarks>
    /// <param name="list">The list to get the blocks from.</param>
    /// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
    public static IEnumerable<Prefab> GetBlocks(this PrefabList list)
        => list.Prefabs.Where(group => group.Type != PrefabType.Level);

    /// <summary>
    /// Gets all of the blocks from the prefab list.
    /// </summary>
    /// <remarks>
    /// <see cref="PartialPrefab.Type"/> != <see cref="PrefabType.Level"/>.
    /// </remarks>
    /// <param name="list">The list to get the blocks from.</param>
    /// <returns><see cref="IEnumerable{T}"/> iterating over the blocks in <paramref name="list"/>.</returns>
    public static IEnumerable<PartialPrefab> GetBlocks(this PartialPrefabList list)
        => list.Prefabs.Where(group => group.Type != PrefabType.Level);

    /// <summary>
    /// Removes all empty segments from a prefab.
    /// </summary>
    /// <param name="list">The list to remove the segments from.</param>
    /// <param name="id">Id of the prefab.</param>
    /// <param name="keepInPlace">
    /// If <see langword="true"/>, the prefab will be moved back by shift from <see cref="Prefab.Remove(int3, out PrefabSegment, out int3)"/>,
    /// if <see langword="false"/>, the prefab may move.
    /// </param>
    /// <param name="cache">Cache of the instances of the prefab, must be created from this <see cref="PrefabList"/> and must represent the current state of the prefabs.</param>
    /// <returns>How many segments were removed.</returns>
    public static int RemoveEmptySegmentsFromPrefab(this PrefabList list, ushort id, bool keepInPlace = true, BlockInstancesCache? cache = null)
    {
        var prefab = list.GetPrefab(id);

        int removedCount = 0;

        Debug.Assert(prefab.Count <= 4 * 4 * 4, "prefab.Count should be smaller that it's max size.");
        Span<int3> toRemove = stackalloc int3[4 * 4 * 4];

        foreach (var (pos, segment) in prefab)
        {
            if (segment.IsEmpty)
            {
                toRemove[removedCount++] = pos;
            }
        }

        foreach (var pos in toRemove[..removedCount])
        {
            list.RemoveSegmentFromPrefab(id, pos, keepInPlace, cache);
        }

        return removedCount;
    }
}

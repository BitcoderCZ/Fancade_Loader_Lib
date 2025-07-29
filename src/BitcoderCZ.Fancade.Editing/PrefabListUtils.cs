// <copyright file="PrefabListUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Partial;
using MathUtils.Vectors;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BitcoderCZ.Fancade.Editing;

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

        foreach (var segment in prefab.OrderedValues.Reverse())
        {
            if (segment.IsEmpty)
            {
                toRemove[removedCount++] = segment.PosInPrefab;
            }
        }

        foreach (var pos in toRemove[..removedCount])
        {
            list.RemoveSegmentFromPrefab(id, pos, keepInPlace, cache);
        }

        return removedCount;
    }

    /// <summary>
    /// Adds the connections between blocks that are rigth next to each other.
    /// </summary>
    /// <param name="list">The list to operate on.</param>
    /// <param name="terminalInfos"><see cref="PrefabTerminalInfo"/>s for <paramref name="list"/> <b>AND</b> <see cref="StockBlocks.PrefabList"/>.</param>
    public static void AddImplicitConnections(this PrefabList list, FrozenDictionary<ushort, PrefabTerminalInfo>? terminalInfos = null)
    {
        var stockPrefabs = StockBlocks.PrefabList;

        terminalInfos ??= PrefabTerminalInfo.Create(stockPrefabs.Concat(list));

        HashSet<(ushort3, byte3)> connectionsFrom = [];
        HashSet<(ushort3, byte3)> connectionsTo = [];

        foreach (var item in list.Prefabs)
        {
            if (item.Blocks.Size == int3.Zero)
            {
                continue;
            }

            foreach (var connection in item.Connections)
            {
                connectionsFrom.Add((connection.From, (byte3)connection.FromVoxel));
                connectionsTo.Add((connection.To, (byte3)connection.ToVoxel));
            }

            var blocks = item.Blocks;
            for (int z = 0; z < blocks.Size.Z; z++)
            {
                for (int y = 0; y < blocks.Size.Y; y++)
                {
                    for (int x = 0; x < blocks.Size.X; x++)
                    {
                        ushort3 pos = new ushort3(x, y, z);
                        ushort id = blocks.GetBlockUnchecked(pos);

                        if (id == 0)
                        {
                            continue;
                        }

                        if (!stockPrefabs.TryGetPrefab(id, out var prefab) && !list.TryGetPrefab(id, out prefab))
                        {
                            continue;
                        }

                        if (!terminalInfos.TryGetValue(id, out var infos) || infos.Terminals.IsEmpty)
                        {
                            continue;
                        }

                        foreach (var info in infos.InputTerminals)
                        {
                            if (info.IsInput ? connectionsTo.Contains((pos, info.Position)) : connectionsFrom.Contains((pos, info.Position)))
                            {
                                continue;
                            }

                            if (TryGetImplicitlyConnectedTerminalPos(pos, info, blocks, out var otherBlockPos, out var otherTerminalPos))
                            {
                                item.Connections.Add(info.IsInput
                                    ? new Connection(otherBlockPos, pos, otherTerminalPos, info.Position)
                                    : new Connection(pos, otherBlockPos, info.Position, otherTerminalPos));
                            }
                        }
                    }
                }
            }

            connectionsFrom.Clear();
            connectionsTo.Clear();
        }

        bool TryGetImplicitlyConnectedTerminalPos(ushort3 pos, TerminalInfo info, BlockData blocks, out ushort3 otherBlockPos, out byte3 otherTerminalPos)
        {
            ushort3 otherPos = pos + (info.Position / 8) + info.Direction.GetOffset();

            ushort otherId = blocks.GetBlockOrDefault(otherPos);
            if (otherId == 0)
            {
                otherBlockPos = default;
                otherTerminalPos = default;
                return false;
            }

            if (stockPrefabs.TryGetSegments(otherId, out var segment) || list.TryGetSegments(otherId, out segment))
            {
                otherBlockPos = (ushort3)(otherPos - segment.PosInPrefab);
                otherTerminalPos = info.Direction switch
                {
                    TerminalDirection.PositiveX => new byte3(0, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.PositiveZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, 0),
                    TerminalDirection.NegativeX => new byte3(((segment.PosInPrefab.X + 1) * 8) - 2, info.Position.Y, (segment.PosInPrefab.Z * 8) + (info.Position.Z % 8)),
                    TerminalDirection.NegativeZ => new byte3((segment.PosInPrefab.X * 8) + (info.Position.X % 8), info.Position.Y, ((segment.PosInPrefab.Z + 1) * 8) - 2),
                    _ => throw new UnreachableException(),
                };

                var otherInfos = terminalInfos[segment.PrefabId];

                byte3 otherTerminalPosLocal = otherTerminalPos;
                if (otherInfos.Terminals.Any(item => item.Position == otherTerminalPosLocal && item.Type == info.Type))
                {
                    return true;
                }
            }

            otherBlockPos = default;
            otherTerminalPos = default;
            return false;
        }
    }
}

// <copyright file="PrefabListUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Partial;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Maths.Vectors;
using System.Collections.Frozen;
using System.Diagnostics;

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
    public static void AddImplicitConnections(this PrefabList list, FrozenDictionary<ushort, PrefabTerminalInfo>? terminalInfos = null) // TODO: add tests
    {
        var stockPrefabs = StockBlocks.PrefabList;

        if (list.IdOffset == 0)
        {
            // is stock
            terminalInfos ??= PrefabTerminalInfo.Create(list, id =>
            {
                return list.TryGetSegment(id, out var segment) && list.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null;
            });
        }
        else
        {
            terminalInfos ??= PrefabTerminalInfo.Create(stockPrefabs.Concat(list), id =>
            {
                if (id < RawGame.CurrentNumbStockPrefabs)
                {
                    return stockPrefabs.TryGetSegment(id, out var segment) && stockPrefabs.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null;
                }
                else
                {
                    return list.TryGetSegment(id, out var segment) && list.TryGetPrefab(segment.PrefabId, out var prefab) ? prefab : null;
                }
            });
        }

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

        bool TryGetImplicitlyConnectedTerminalPos(ushort3 pos, TerminalInfo terminal, BlockData blocks, out ushort3 otherBlockPos, out byte3 otherTerminalPos)
        {
            var otherPosVoxel = (pos * Voxels.Size) + terminal.Position + (terminal.Direction.GetOffset() * 2);
            var otherPos = VoxelToBlock(otherPosVoxel);

            ushort otherId = blocks.GetBlockOrDefault(otherPos);
            if (otherId == 0)
            {
                otherBlockPos = default;
                otherTerminalPos = default;
                return false;
            }

            if (stockPrefabs.TryGetSegment(otherId, out var segment) || list.TryGetSegment(otherId, out segment))
            {
                var otherBlockPosLocal = otherPos - segment.PosInPrefab;
                if (otherBlockPosLocal.X < 0 || otherBlockPosLocal.Y < 0 || otherBlockPosLocal.Z < 0)
                {
                    otherBlockPos = default;
                    otherTerminalPos = default;
                    return false;
                }

                var otherTerminalPosLocal = otherPosVoxel - (otherBlockPosLocal * 8);

                var otherTerminals = terminalInfos[segment.PrefabId];

                if (otherTerminals.Terminals.Any(item => item.Position == otherTerminalPosLocal && item.IsInput != terminal.IsInput && SignalTypeUtils.CanConnect(terminal.Type, item.Type, terminal.IsInput)))
                {
                    otherBlockPos = (ushort3)otherBlockPosLocal;
                    otherTerminalPos = (byte3)otherTerminalPosLocal;
                    return true;
                }
            }

            otherBlockPos = default;
            otherTerminalPos = default;
            return false;

            static int3 VoxelToBlock(int3 voxel)
            {
                return new int3(voxel.X >> 3, voxel.Y >> 3, voxel.Z >> 3);
            }
        }
    }

    /// <summary>
    /// Gets the prefab with the specified id from <see cref="StockBlocks.PrefabList"/> or <paramref name="list"/>.
    /// </summary>
    /// <param name="list">The list to get the prefab from, <see cref="PrefabList.IdOffset"/> must be equal to <see cref="RawGame.CurrentNumbStockPrefabs"/>.</param>
    /// <param name="id">Id of the prefab to get.</param>
    /// <returns>The prefab with the specified id.</returns>
    public static Prefab GetPrefabOrStock(this PrefabList list, ushort id)
        => id < RawGame.CurrentNumbStockPrefabs ? StockBlocks.PrefabList.GetPrefab(id) : list.GetPrefab(id);

    /// <summary>
    /// Gets the segment with the specified id from <see cref="StockBlocks.PrefabList"/> or <paramref name="list"/>.
    /// </summary>
    /// <param name="list">The list to get the segment from, <see cref="PrefabList.IdOffset"/> must be equal to <see cref="RawGame.CurrentNumbStockPrefabs"/>.</param>
    /// <param name="id">Id of the segment to get.</param>
    /// <returns>The segment with the specified id.</returns>
    public static PrefabSegment GetSegmentOrStock(this PrefabList list, ushort id)
        => id < RawGame.CurrentNumbStockPrefabs ? StockBlocks.PrefabList.GetSegment(id) : list.GetSegment(id);
}

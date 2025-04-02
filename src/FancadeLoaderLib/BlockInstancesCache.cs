using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib;

/// <summary>
/// Caches the positions of a certain block.
/// </summary>
public sealed class BlockInstancesCache : IEnumerable<(Prefab Prefab, IEnumerable<int3> Position)>
{
    private readonly List<(Prefab Prefab, List<int3> Position)> _instances = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockInstancesCache"/> class.
    /// </summary>
    /// <param name="prefabs">The prefabs to scan for the block id.</param>
    /// <param name="blockId">Id of the block to find.</param>
    public BlockInstancesCache(IEnumerable<Prefab> prefabs, ushort blockId)
    {
        BLockId = blockId;

        IsEmpty = true;

#if NET9_0_OR_GREATER
        Lock instancesLock = new();
#else
        object instancesLock = new();
#endif

        Parallel.ForEach(prefabs, prefab =>
        {
            List<int3> positions = [];

            var blocks = prefab.Blocks;
            for (int z = 0; z < blocks.Size.Z; z++)
            {
                for (int y = 0; y < blocks.Size.Y; y++)
                {
                    for (int x = 0; x < blocks.Size.X; x++)
                    {
                        int3 pos = new int3(x, y, z);

                        if (blocks.GetBlockUnchecked(pos) == blockId)
                        {
                            positions.Add(pos);
                            IsEmpty = false;
                        }
                    }
                }
            }

            lock (instancesLock)
            {
                _instances.Add((prefab, positions));
            }
        });
    }

    /// <summary>
    /// Gets the id of the block this <see cref="BlockInstancesCache"/> was created for.
    /// </summary>
    /// <value>Id of the block this <see cref="BlockInstancesCache"/> was created for.</value>
    public ushort BLockId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="BlockInstancesCache"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if no blocks with id <see cref="BLockId"/> were found, when this <see cref="BlockInstancesCache"/> was created; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty { get; private set; }

    /// <summary>
    /// Enumerates the <see cref="PrefabInstances"/> of the <see cref="BlockInstancesCache"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> that iterates over the <see cref="PrefabInstances"/> of the <see cref="BlockInstancesCache"/>.</returns>
    public IEnumerable<PrefabInstances> EnumeratePrefabInstances()
    {
        foreach (var (prefab, positions) in _instances)
        {
            yield return new PrefabInstances(prefab, positions);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<(Prefab Prefab, IEnumerable<int3> Position)> GetEnumerator()
        => _instances.Select(item => (item.Prefab, (IEnumerable<int3>)item.Position)).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    internal void MovePositions(int3 move)
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (_, positions) in _instances)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] += move;
            }
        }
    }

    internal void RemoveBlock()
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                prefab.Blocks.SetBlock(pos, 0);
            }
        }
    }

    internal void RemoveBlock(int3 offset)
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                prefab.Blocks.SetBlock(pos + offset, 0);
            }
        }
    }

    internal void RemoveBlocks(ReadOnlySpan<int3> offsets)
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                foreach (var offset in offsets)
                {
                    prefab.Blocks.SetBlock(pos + offset, 0);
                }
            }
        }
    }

    internal void AddBlock(PrefabList list, int3 offset, ushort id)
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                ushort idOld = prefab.Blocks.GetBlockOrDefault(pos + offset);

                if (idOld != 0 && list.TryGetPrefab(idOld, out var oldPrefab))
                {
                    int3 prefabPos = (pos + offset) - list.GetSegment(idOld).PosInPrefab;

                    foreach (var segPos in oldPrefab.Keys)
                    {
                        prefab.Blocks.SetBlock(prefabPos + segPos, 0);
                    }
                }

                prefab.Blocks.SetBlock(pos + offset, id);
            }
        }
    }

    internal void AddBlocks(PrefabList list, ReadOnlySpan<(int3 Offset, ushort Id)> ids)
    {
        if (IsEmpty)
        {
            return;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                foreach (var (offset, id) in ids)
                {
                    ushort idOld = prefab.Blocks.GetBlockOrDefault(pos + offset);

                    if (idOld != 0 && list.TryGetPrefab(idOld, out var oldPrefab))
                    {
                        int3 prefabPos = (pos + offset) - list.GetSegment(idOld).PosInPrefab;

                        foreach (var segPos in oldPrefab.Keys)
                        {
                            prefab.Blocks.SetBlock(prefabPos + segPos, 0);
                        }
                    }

                    prefab.Blocks.SetBlock(pos + offset, id);
                }
            }
        }
    }

    internal bool CanAddBlock(int3 offset, out BlockObstructionInfo obstructionInfo)
    {
        obstructionInfo = default;

        if (IsEmpty)
        {
            return true;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                if (prefab.Blocks.GetBlockOrDefault(pos + offset) != 0)
                {
                    obstructionInfo = new BlockObstructionInfo(prefab.Name, pos, pos + offset);
                    return false;
                }
            }
        }

        return true;
    }

    internal bool CanAddBlocks(ReadOnlySpan<int3> offsets, out BlockObstructionInfo obstructionInfo)
    {
        obstructionInfo = default;

        if (IsEmpty)
        {
            return true;
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                foreach (var offset in offsets)
                {
                    if (prefab.Blocks.GetBlockOrDefault(pos + offset) != 0)
                    {
                        obstructionInfo = new BlockObstructionInfo(prefab.Name, pos, pos + offset);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    internal void MoveBlock(ReadOnlySpan<int3> removeOffsets, ReadOnlySpan<(int3 Offset, ushort Id)> ids, bool checkForObstructions)
    {
        if (checkForObstructions)
        {
            ThrowNotImplementedException();
        }

        foreach (var (prefab, positions) in _instances)
        {
            foreach (var pos in positions)
            {
                foreach (var offset in removeOffsets)
                {
                    prefab.Blocks.SetBlockUnchecked(pos + offset, 0);
                }

                foreach (var (offset, id) in ids)
                {
                    prefab.Blocks.SetBlock(pos + offset, id);
                }
            }
        }
    }

    /// <summary>
    /// Represents the instances of a block in a prefab.
    /// </summary>
    public readonly struct PrefabInstances
    {
        private readonly List<int3> _positions;

        internal PrefabInstances(Prefab prefab, List<int3> positions)
        {
            Prefab = prefab;
            _positions = positions;
        }

        /// <summary>
        /// Gets the prefab the instances are in.
        /// </summary>
        /// <value>The prefab the instances are in.</value>
        public Prefab Prefab { get; }

        /// <summary>
        /// Gets the positions of the instances.
        /// </summary>
        /// <value>Posititons of the instances.</value>
        public ReadOnlySpan<int3> Positions => CollectionsMarshal.AsSpan(_positions);

        /// <summary>
        /// Deconstructs the <see cref="BlockInstancesCache"/>.
        /// </summary>
        /// <param name="prefab">The prefab the instances are in.</param>
        /// <param name="positions">Posititons of the instances.</param>
        public void Deconstruct(out Prefab prefab, out ReadOnlySpan<int3> positions)
        {
            prefab = Prefab;
            positions = Positions;
        }
    }
}

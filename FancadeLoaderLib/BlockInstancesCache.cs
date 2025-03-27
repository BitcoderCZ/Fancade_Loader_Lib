using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FancadeLoaderLib;

/// <summary>
/// Caches the positions of a certain block.
/// </summary>
public sealed class BlockInstancesCache : IEnumerable<(Prefab Prefab, IEnumerable<int3> Position)>
{
    private readonly List<(Prefab Prefab, IEnumerable<int3> Position)> _instances = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockInstancesCache"/> class.
    /// </summary>
    /// <param name="prefabs">The prefabs to scan for the block id.</param>
    /// <param name="blockId">Id of the block to find.</param>
    public BlockInstancesCache(IEnumerable<Prefab> prefabs, ushort blockId)
    {
        BLockId = blockId;

        IsEmpty = true;

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

            _instances.Add((prefab, positions));
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

    /// <inheritdoc/>
    public IEnumerator<(Prefab Prefab, IEnumerable<int3> Position)> GetEnumerator()
        => _instances.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

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

    internal bool CanAddBlock(int3 offset)
    {
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
                    return false;
                }
            }
        }

        return true;
    }
}

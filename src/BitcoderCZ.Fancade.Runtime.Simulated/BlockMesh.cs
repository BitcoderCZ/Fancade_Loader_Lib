// <copyright file="BlockMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Maths.Vectors;
using Microsoft.Extensions.ObjectPool;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores the mesh of the inside of a prefab.
/// </summary>
public readonly struct BlockMesh
{
    /// <summary>
    /// An empty <see cref="BlockMesh"/> instance.
    /// </summary>
    public static readonly BlockMesh Empty = new BlockMesh(0, new Array3D<int>(int3.Zero), [], []);

    private static readonly DefaultObjectPool<Stack<(ushort SegmentId, int3 Pos, ushort MeshIndex)>> _stackPool = new DefaultObjectPool<Stack<(ushort, int3, ushort)>>(new PooledStackPolicy<(ushort, int3, ushort)>() { DefaultCapacity = 256, });

    private static readonly short3[] NeighborOffsets =
    [
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    ];

    private readonly short[] _blockMeshIds;
    private readonly List<(FcMesh Mesh, int UniqueMeshIndex)> _meshes;
    private readonly Array3D<int> _blockMeshIdOffsets;

    private BlockMesh(int meshCount, Array3D<int> blockMeshIdOffsets, List<(FcMesh Mesh, int UniqueMeshIndex)> meshes, short[] blockMeshIds)
    {
        Debug.Assert(meshes.Count == meshCount, $"{nameof(meshes)} should have {meshCount} elements.");

        MeshCount = meshCount;
        _blockMeshIdOffsets = blockMeshIdOffsets;
        _meshes = meshes;
        _blockMeshIds = blockMeshIds;
    }

    /// <summary>
    /// Gets the number of meshes.
    /// </summary>
    /// <value>Number of meshes.</value>
    public readonly int MeshCount { get; }

    /// <summary>
    /// Gets the size of the inside of the prefab.
    /// </summary>
    /// <value>Size of the inside of the prefab.</value>
    public readonly int3 Size => _blockMeshIdOffsets.Size;

    /// <summary>
    /// Gets the mesh offsets.
    /// </summary>
    /// <value>Mesh offsets.</value>
    public readonly ReadOnlySpan<int> BlockMeshIdOffsets => _blockMeshIdOffsets.Array;

    /// <summary>
    /// Gets the mesh ids as <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <value>Mesh ids as <see cref="ReadOnlySpan{T}"/>.</value>
    public readonly ReadOnlySpan<short> BlockMeshIds => _blockMeshIds;

    /// <summary>
    /// Gets the mesh ids.
    /// </summary>
    /// <value>Mesh ids.</value>
    public readonly IReadOnlyList<short> BlockMeshIdsList => _blockMeshIds;

    /// <summary>
    /// Gets the meshes as <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <value>The meshes as <see cref="ReadOnlySpan{T}"/>.</value>
    public readonly ReadOnlySpan<(FcMesh Mesh, int UniqueMeshIndex)> Meshes => CollectionsMarshal.AsSpan(_meshes);

    /// <summary>
    /// Gets the meshes.
    /// </summary>
    /// <value>The meshes.</value>
    public readonly IReadOnlyList<(FcMesh Mesh, int UniqueMeshIndex)> MeshesList => _meshes;

    /// <summary>
    /// Creates a new <see cref="BlockMesh"/> instance.
    /// </summary>
    /// <param name="blocks">The <see cref="BlockData"/> to create the <see cref="BlockMesh"/> for.</param>
    /// <param name="createScriptMesh">Whether to create mesh for script blocks.</param>
    /// <param name="prefabs">A <see cref="PrefabList"/> used to resolve prefab types and voxels.</param>
    /// <param name="segmentMeshes">A <see cref="ReadOnlySpan{T}"/> of <see cref="PrefabSegmentMeshes"/>, where the index corresponds to the segment id.</param>
    /// <param name="getUnique">A method that given a mesh (blocks, mesh index), gets it's unique index, and if it was already encountered, returns the stored mesh (the input list can be reused).</param>
    /// <returns>The created <see cref="BlockMesh"/>.</returns>
    public static BlockMesh Create(BlockData blocks, bool createScriptMesh, PrefabList prefabs, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes, Func<ValueListWithHash<FcMesh.Block>, int, (int UniqueIndex, ValueList<FcMesh.Block>? UniqueMesh)> getUnique)
    {
        if (blocks.Size == int3.Zero)
        {
            return Empty;
        }

        var blocksSize = blocks.Array.Size;
        int blocksLength = blocksSize.X * blocksSize.Y * blocksSize.Z;

        int totalSegmentMeshCount = createScriptMesh ? 1 : 0;

        var blockMeshIdOffsets = new Array3D<int>(blocksSize);

        ushort[] blocksArray = blocks.Array.Array;
        for (int i = 0; i < blocksLength; i++)
        {
            blockMeshIdOffsets[i] = totalSegmentMeshCount;

            ushort segmentId = blocksArray[i];

            if (segmentId == 0 || (!createScriptMesh && (segmentId < RawGame.CurrentNumbStockPrefabs ? StockBlocks.IsScriptSegment(segmentId) : prefabs.GetPrefab(prefabs.GetSegment(segmentId).PrefabId).Type == PrefabType.Script)))
            {
                continue;
            }

            totalSegmentMeshCount += segmentMeshes[segmentId].MeshCount;
        }

        if (totalSegmentMeshCount == 0)
        {
            return Empty;
        }

        var stockPrefabs = StockBlocks.PrefabList;

        short[] blockMeshIds = new short[totalSegmentMeshCount];
        var meshes = new List<(FcMesh Mesh, int UniqueMeshIndex)>(totalSegmentMeshCount);

        blockMeshIds.AsSpan().Fill(-1);

        Stack<(ushort SegmentId, int3 Pos, ushort MeshIndex)> stack = _stackPool.Get();

        ValueListWithHash<FcMesh.Block> blockList = default;

        if (createScriptMesh)
        {
            const int ScriptMeshIndex = 0;

            for (int blockIndex = 0; blockIndex < blocksLength; blockIndex++)
            {
                ushort blockId = blocksArray[blockIndex];

                if (blockId is 0)
                {
                    continue;
                }

                int3 blockPos = blocks.Array.Index(blockIndex);

                var segment = GetSegment(blockId);

                var prefab = GetPrefab(segment.PrefabId);

                var segmentMesh = segmentMeshes[blockId];

                if (prefab.Type is not PrefabType.Script)
                {
                    continue;
                }

                int currentBLockIndex = blockPos.X + ((blockPos.Y + (blockPos.Z * blocksSize.Y)) * blocksSize.X);

                for (int segmentMeshIndex = 0; segmentMeshIndex < segmentMesh.MeshCount; segmentMeshIndex++)
                {
                    Debug.Assert(blockMeshIds[segmentMeshIndex + blockMeshIdOffsets[blockIndex]] is -1);
                    blockMeshIds[blockMeshIdOffsets[currentBLockIndex] + segmentMeshIndex] = ScriptMeshIndex;

                    blockList.List.Add(new FcMesh.Block(blockId, blockPos, (ushort)segmentMeshIndex));
                }
            }

            int3 minPos;
            if (blockList.List.Count is 0)
            {
                minPos = int3.Zero;
            }
            else
            {
                minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var block in blockList.List)
                {
                    minPos = int3.Min(minPos, block.Offset);
                }

                // make offsets zero based
                if (minPos != int3.Zero)
                {
                    for (int i = blockList.List.Count - 1; i >= 0; i--)
                    {
                        ref var block = ref blockList.List.GetRef(i);
                        block = new FcMesh.Block(block.SegmentId, block.Offset - minPos, block.LocalMeshIndex);
                    }
                }

                blockList.List.Sort();
            }

            blockList.ComputeHash();

            var (uniqueIndex, uniqueMesh) = getUnique(blockList, ScriptMeshIndex);

            if (uniqueMesh is null)
            {
                // new unique, blockList was added to unique mesh list
                meshes.Add((new FcMesh(blockList.List, minPos), uniqueIndex));
                blockList = default;
            }
            else
            {
                // existing mesh, blockList can be reused
                meshes.Add((new FcMesh(uniqueMesh.Value, minPos), uniqueIndex));
                blockList.List.Clear();
            }
        }

        short meshIndex = createScriptMesh ? (short)1 : (short)0; // returned
        for (int blockIndex = 0; blockIndex < blocksLength; blockIndex++)
        {
            ushort blockId = blocksArray[blockIndex];

            if (blockId == 0)
            {
                continue;
            }

            int3 blockPos = blocks.Array.Index(blockIndex);

            var segment = GetSegment(blockId);

            var prefab = GetPrefab(segment.PrefabId);

            var segmentMesh = segmentMeshes[blockId];

            if (prefab.Type is PrefabType.Script)
            {
                continue;
            }

            for (int segmentMeshIndex = 0; segmentMeshIndex < segmentMesh.MeshCount; segmentMeshIndex++)
            {
                if (blockMeshIds[segmentMeshIndex + blockMeshIdOffsets[blockIndex]] != -1)
                {
                    continue;
                }

                Debug.Assert(stack.Count == 0, $"{nameof(stack)} should be empty.");

                stack.Push((blockId, blockPos, (ushort)segmentMeshIndex));

                while (stack.TryPop(out var item))
                {
                    var currentPos = item.Pos;
                    blockList.List.Add(new FcMesh.Block(item.SegmentId, currentPos, item.MeshIndex));

                    int currentBLockIndex = currentPos.X + ((currentPos.Y + (currentPos.Z * blocksSize.Y)) * blocksSize.X);

                    ushort currentBlockId = blocksArray[currentBLockIndex];

                    blockMeshIds[blockMeshIdOffsets[currentBLockIndex] + item.MeshIndex] = meshIndex;

                    for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                    {
                        int3 neighborPos = currentPos + NeighborOffsets[sideIndex];

                        if (!neighborPos.InBounds(blocksSize.X, blocksSize.Y, blocksSize.Z))
                        {
                            continue;
                        }

                        int neighborIndex = neighborPos.X + ((neighborPos.Y + (neighborPos.Z * blocksSize.Y)) * blocksSize.X);

                        ushort neighborId = blocksArray[neighborIndex];

                        int neighborMeshCount = segmentMeshes[neighborId].MeshCount;

                        if (neighborId is 0 ||
                            GetPrefab(GetSegment(neighborId).PrefabId).Type is PrefabType.Script ||
                            neighborMeshCount is 0)
                        {
                            continue;
                        }

                        int neighborMaxMeshCountUpToPos = blockMeshIdOffsets[neighborIndex];

                        for (ushort neighborMeshIndex = 0; neighborMeshIndex < neighborMeshCount; neighborMeshIndex++)
                        {
                            if (blockMeshIds[neighborMeshIndex + neighborMaxMeshCountUpToPos] is -1 &&
                                Glues(currentBlockId, item.MeshIndex, sideIndex, neighborId, neighborMeshIndex, segmentMeshes))
                            {
                                stack.Push((neighborId, neighborPos, neighborMeshIndex));
                            }
                        }
                    }
                }

                int3 minPos = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var block in blockList.List)
                {
                    minPos = int3.Min(minPos, block.Offset);
                }

                // make offsets zero based
                for (int i = blockList.List.Count - 1; i >= 0; i--)
                {
                    ref var block = ref blockList.List.GetRef(i);
                    block = new FcMesh.Block(block.SegmentId, block.Offset - minPos, block.LocalMeshIndex);
                }

                blockList.List.Sort();

                blockList.ComputeHash();

                var (uniqueIndex, uniqueMesh) = getUnique(blockList, meshIndex);

                if (uniqueMesh is null)
                {
                    // new unique, blockList was added to unique mesh list
                    meshes.Add((new FcMesh(blockList.List, minPos), uniqueIndex));
                    blockList = default;
                }
                else
                {
                    // existing mesh, blockList can be reused
                    meshes.Add((new FcMesh(uniqueMesh.Value, minPos), uniqueIndex));
                    blockList.List.Clear();
                }

                meshIndex++;
            }
        }

        _stackPool.Return(stack);

        return new BlockMesh(meshIndex, blockMeshIdOffsets, meshes, blockMeshIds);

        PrefabSegment GetSegment(ushort id)
        {
            return id < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetSegment(id) : prefabs.GetSegment(id);
        }

        Prefab GetPrefab(ushort prefabId)
        {
            return prefabId < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetPrefab(prefabId) : prefabs.GetPrefab(prefabId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Glues(ushort currentBlockId, int currentMeshIndex, int sideIndex, ushort neighborBLockId, int neighborMeshIndex, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes)
        {
            if (sideIndex >= 6)
            {
                return false;
            }

            var meshA = segmentMeshes[currentBlockId].Meshes[currentMeshIndex];
            var meshB = segmentMeshes[neighborBLockId].Meshes[neighborMeshIndex];

            int neighborSide = sideIndex ^ 1;

            return (meshA.GetSideGlue(sideIndex) & meshB.GetSideGlue(neighborSide)) != 0;
        }
    }

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffset(int3 position)
        => _blockMeshIdOffsets.Get(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>, without bounds checking.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffsetUnchecked(int3 position)
        => _blockMeshIdOffsets.GetUnchecked(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshOffsetOrZero(int3 position)
        => _blockMeshIdOffsets.InBounds(position)
        ? _blockMeshIdOffsets.GetUnchecked(position)
        : 0;

    /// <summary>
    /// Gets the id of the mesh at <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="segmentMeshIndex">The local segment mesh index of the block.</param>
    /// <returns>Id of the mesh at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMeshAtPos(int3 position, int segmentMeshIndex)
        => _blockMeshIds[GetMeshOffset(position) + segmentMeshIndex];

    /// <summary>
    /// Gets the positions a certain mesh occupies, may contain duplicates.
    /// </summary>
    /// <param name="meshIndex">Index of the mesh whose positions should be retrieved.</param>
    /// <returns>Positions the mesh occupies, may contain duplicates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly FcMesh.PositionsEnumerable EnumerateMeshBlocks(int meshIndex)
        => _meshes[meshIndex].Mesh.Positions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetMesh(int meshIndex, out FcMesh mesh, out int uniqueMeshIndex)
    {
        var item = _meshes[meshIndex];
        mesh = item.Mesh;
        uniqueMeshIndex = item.UniqueMeshIndex;
    }
}
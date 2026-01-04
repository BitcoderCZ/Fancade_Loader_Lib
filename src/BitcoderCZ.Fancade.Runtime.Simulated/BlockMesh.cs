// <copyright file="BlockMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Fancade.Editing;
using BitcoderCZ.Fancade.Raw;
using BitcoderCZ.Fancade.Runtime.Simulated.Utils;
using BitcoderCZ.Maths.Vectors;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    private readonly List<ValueList<int3>> _meshBlockPositions;
    private readonly Array3D<int> _blockMeshIdOffsets;

    private BlockMesh(int meshCount, Array3D<int> blockMeshIdOffsets, List<ValueList<int3>> meshBlockPositions, short[] blockMeshIds)
    {
        Debug.Assert(meshBlockPositions.Count == meshCount, $"{nameof(meshBlockPositions)} should have {meshCount} elements.");

        MeshCount = meshCount;
        _blockMeshIdOffsets = blockMeshIdOffsets;
        _meshBlockPositions = meshBlockPositions;
        _blockMeshIds = blockMeshIds;
    }

    /// <summary>
    /// Gets the number of meshes.
    /// </summary>
    /// <value>Number of meshes.</value>
    public int MeshCount { get; }

    /// <summary>
    /// Gets the size of the inside of the prefab.
    /// </summary>
    /// <value>Size of the inside of the prefab.</value>
    public int3 Size => _blockMeshIdOffsets.Size;

    /// <summary>
    /// Gets the mesh offsets.
    /// </summary>
    /// <value>Mesh offsets.</value>
    public ReadOnlySpan<int> BlockMeshIdOffsets => _blockMeshIdOffsets.Array;

    /// <summary>
    /// Gets the mesh ids.
    /// </summary>
    /// <value>Mesh ids.</value>
    public ReadOnlySpan<short> BlockMeshIds => _blockMeshIds;

    /// <summary>
    /// Creates a new <see cref="BlockMesh"/> instance.
    /// </summary>
    /// <param name="blocks">The <see cref="BlockData"/> to create the <see cref="BlockMesh"/> for.</param>
    /// <param name="prefabs">A <see cref="PrefabList"/> used to resolve prefab types and voxels.</param>
    /// <param name="segmentMeshes">A <see cref="ReadOnlySpan{T}"/> of <see cref="PrefabSegmentMeshes"/>, where the index corresponds to the segment id.</param>
    /// <returns>The created <see cref="BlockMesh"/>.</returns>
    public static BlockMesh Create(BlockData blocks, PrefabList prefabs, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes)
    {
        if (blocks.Size == int3.Zero)
        {
            return Empty;
        }

        var blocksSize = blocks.Array.Size;
        int blocksLength = blocksSize.X * blocksSize.Y * blocksSize.Z;

        int totalSegmentMeshCount = 0;

        var blockMeshIdOffsets = new Array3D<int>(blocksSize);

        ushort[] blocksArray = blocks.Array.Array;
        for (int i = 0; i < blocksLength; i++)
        {
            blockMeshIdOffsets[i] = totalSegmentMeshCount;

            ushort segmentId = blocksArray[i];

            if (segmentId == 0 || (segmentId < RawGame.CurrentNumbStockPrefabs ? StockBlocks.IsScriptSegment(segmentId) : prefabs.GetPrefab(prefabs.GetSegment(segmentId).PrefabId).Type == PrefabType.Script))
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
        var meshBlockPositions = new List<ValueList<int3>>(totalSegmentMeshCount / 16);

        blockMeshIds.AsSpan().Fill(-1);

        Stack<(int3 Pos, short MeshIndex)> stack = new(blocksLength * 6);

        short meshIndex = 0; // returned
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

            if (prefab.Type == PrefabType.Script)
            {
                // TODO
            }
            else
            {
                for (int segmentMeshIndex = 0; segmentMeshIndex < segmentMesh.MeshCount; segmentMeshIndex++)
                {
                    if (blockMeshIds[segmentMeshIndex + blockMeshIdOffsets[blockIndex]] != -1)
                    {
                        continue;
                    }

                    Debug.Assert(stack.Count == 0, $"{nameof(stack)} should be empty.");

                    stack.Push((blockPos, (short)segmentMeshIndex));

                    ValueList<int3> meshPositions = [];
                    while (stack.TryPop(out var item))
                    {
                        var currentPos = item.Pos;
                        meshPositions.Add(currentPos);

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

                            if (neighborId == 0 ||
                                GetPrefab(GetSegment(neighborId).PrefabId).Type == PrefabType.Script ||
                                neighborMeshCount == 0)
                            {
                                continue;
                            }

                            int neighborMaxMeshCountUpToPos = blockMeshIdOffsets[neighborIndex];

                            for (short neighborMeshIndex = 0; neighborMeshIndex < neighborMeshCount; neighborMeshIndex++)
                            {
                                if (blockMeshIds[neighborMeshIndex + neighborMaxMeshCountUpToPos] == -1 &&
                                    Glues(currentBlockId, item.MeshIndex, sideIndex, neighborId, neighborMeshIndex, segmentMeshes))
                                {
                                    stack.Push((neighborPos, neighborMeshIndex));
                                }
                            }
                        }
                    }

                    meshBlockPositions.Add(meshPositions);
                    meshIndex++;
                }
            }
        }

        return new BlockMesh(meshIndex, blockMeshIdOffsets, meshBlockPositions, blockMeshIds);

        PrefabSegment GetSegment(ushort id)
        {
            return id < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetSegment(id) : prefabs.GetSegment(id);
        }

        Prefab GetPrefab(ushort prefabId)
        {
            return prefabId < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetPrefab(prefabId) : prefabs.GetPrefab(prefabId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Glues(ushort currentBlockId, short currentMeshIndex, int sideIndex, ushort neighborBLockId, int neighborMeshIndex, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes)
        {
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
    public int GetMeshOffset(int3 position)
        => _blockMeshIdOffsets.Get(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>, without bounds checking.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetMeshOffsetUnchecked(int3 position)
        => _blockMeshIdOffsets.GetUnchecked(position);

    /// <summary>
    /// Gets the mesh offset <paramref name="position"/>.
    /// </summary>
    /// <param name="position">The position of which the mesh offset should be retrieved.</param>
    /// <returns>Mesh offset at <paramref name="position"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetMeshOffsetOrZero(int3 position)
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
    public int GetMeshAtPos(int3 position, int segmentMeshIndex)
        => _blockMeshIds[GetMeshOffset(position) + segmentMeshIndex];

    /// <summary>
    /// Gets the positions a certain mesh occupies.
    /// </summary>
    /// <param name="meshIndex">Index of the mesh whose positions should be retrieved.</param>
    /// <returns>Positions the mesh occupies.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<int3> EnumerateMeshBlocks(int meshIndex)
        => _meshBlockPositions[meshIndex];
}
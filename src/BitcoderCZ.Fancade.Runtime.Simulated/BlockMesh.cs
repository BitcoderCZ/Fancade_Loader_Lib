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

        BitArray?[] attribsCache = new BitArray[RawGame.CurrentNumbStockPrefabs + prefabs.SegmentCount];

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

        BitArray GetAttribs(ushort id)
        {
            var attribs = attribsCache[id];

            if (attribs is not null)
            {
                return attribs;
            }
            else
            {
                var segment = GetSegment(id);
                return attribsCache[id] = segment.Voxels.IsEmpty ? new BitArray(Voxels.VoxelCount * 6) : segment.Voxels.GetFaceGlueInfo();
            }
        }

        bool Glues(ushort currentBlockId, short currentMeshIndex, int sideIndex, ushort neighborBLockId, int neighborMeshIndex, ReadOnlySpan<PrefabSegmentMeshes> segmentMeshes)
        {
            if (sideIndex >= 6)
            {
                return false;
            }

            var meshA = segmentMeshes[currentBlockId].Meshes[currentMeshIndex];
            var meshB = segmentMeshes[neighborBLockId].Meshes[neighborMeshIndex];

            int neighborSide = sideIndex ^ 1;

            ulong glueThis = meshA.GetSideGlue(sideIndex);
            ulong glueNeighbor = meshB.GetSideGlue(neighborSide);
            bool gluesA = (glueThis & glueNeighbor) != 0;

            uint sideIndexShifted = (uint)(1 << (sideIndex & 31));
            var currentMeshInfo = segmentMeshes[currentBlockId];
            var neighborMeshInfo = segmentMeshes[neighborBLockId];
            int sideIndexInverted1 = sideIndex ^ 1;

            BitArray currentVoxels = GetAttribs(currentBlockId);
            BitArray neighborVoxels = GetAttribs(neighborBLockId);

            if ((sideIndexShifted & 3) == 0)
            {
                int someSideIndex = sideIndex << 9;
                if ((sideIndexShifted & 0xc) == 0)
                {
                    bool bVar1 = sideIndex != 4;
                    int uVar8 = 0;
                    if (bVar1)
                    {
                        uVar8 = 0b111000000;
                    }

                    sideIndexInverted1 = uVar8 | (sideIndexInverted1 << 9);

                    int uVar3 = 0b111000000;
                    if (bVar1)
                    {
                        uVar3 = 0;
                    }

                    someSideIndex = uVar3 | someSideIndex;

                    var currentVoxelMeshIndex = currentMeshInfo.VoxelMeshIndex;
                    var neighborVoxelMeshIndex = neighborMeshInfo.VoxelMeshIndex;

                    int lVar5 = uVar3 + 3;

                    for (int someIndex = 0; someIndex < 64; someIndex += 8)
                    {
                        if ((currentVoxelMeshIndex[someIndex + lVar5 + -3] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8] == neighborMeshIndex) &&
                            !currentVoxels[someIndex + someSideIndex] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + -2] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 1] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 1] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 1]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + -1] == currentMeshIndex) &&
                             (neighborVoxelMeshIndex[someIndex + uVar8 + 2] == neighborMeshIndex)
                            && (!currentVoxels[someIndex + someSideIndex + 2]) &&
                           (!neighborVoxels[someIndex + sideIndexInverted1 + 2]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 3] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 3] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 3]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 1] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 4] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 4] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 4]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 2] == currentMeshIndex) &&
                             (neighborVoxelMeshIndex[someIndex + uVar8 + 5] == neighborMeshIndex)
                            && (!currentVoxels[someIndex + someSideIndex + 5]) &&
                           (!neighborVoxels[someIndex + sideIndexInverted1 + 5]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 3] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 6] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 6] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 6]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 4] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 7] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 7] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 7]))
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }
                    }
                }
                else
                {
                    bool bVar1 = (sideIndex & 0xff) != 2;
                    int uVar2 = 0b111000;
                    if (bVar1)
                    {
                        uVar2 = 0;
                    }

                    int uVar4 = 0;
                    if (bVar1)
                    {
                        uVar4 = 0b111000;
                    }

                    int someIndex = sideIndexInverted1 * 512;

                    var currentVoxelMeshIndex = currentMeshInfo.VoxelMeshIndex;
                    var neighborVoxelMeshIndex = neighborMeshInfo.VoxelMeshIndex;

                    for (int j = 0; j < 512; j += 64)
                    {
                        int uVar8 = j | uVar2;
                        sideIndexInverted1 = j | uVar4;
                        if (((currentVoxelMeshIndex[uVar8] == neighborMeshIndex) &&
                              (neighborVoxelMeshIndex[sideIndexInverted1] == neighborMeshIndex) &&
                             (!currentVoxels[uVar8 + someSideIndex]) &&
                            (!neighborVoxels[someIndex + sideIndexInverted1])) ||
                           (currentVoxelMeshIndex[uVar8 | 1] == neighborMeshIndex &&
                               (neighborVoxelMeshIndex[sideIndexInverted1 | 1] == neighborMeshIndex) &&
                              !currentVoxels[(uVar8 | 1) + someSideIndex] &&
                               (!neighborVoxels[(sideIndexInverted1 | 1) + someIndex])) ||
                             (currentVoxelMeshIndex[uVar8 | 2] == neighborMeshIndex &&
                               (neighborVoxelMeshIndex[sideIndexInverted1 | 2] == neighborMeshIndex) &&
                              !currentVoxels[(uVar8 | 2) + someSideIndex] &&
                               (!neighborVoxels[(sideIndexInverted1 | 2) + someIndex])) ||
                            (currentVoxelMeshIndex[uVar8 | 3] == neighborMeshIndex &&
                                  (neighborVoxelMeshIndex[sideIndexInverted1 | 3] == neighborMeshIndex) &&
                                 (!currentVoxels[(uVar8 | 3) + someSideIndex]) &&
                                (!neighborVoxels[(sideIndexInverted1 | 3) + someIndex])) ||
                               (currentVoxelMeshIndex[uVar8 | 4] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 4] == neighborMeshIndex) &&
                                !currentVoxels[(uVar8 | 4) + someSideIndex] &&
                                 (!neighborVoxels[(sideIndexInverted1 | 4) + someIndex])) ||
                              (currentVoxelMeshIndex[uVar8 | 5] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 5] == neighborMeshIndex) &&
                                (!currentVoxels[(uVar8 | 5) + someSideIndex]) &&
                               (!neighborVoxels[(sideIndexInverted1 | 5) + someIndex])) ||
                             (currentVoxelMeshIndex[uVar8 | 6] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 6] == neighborMeshIndex) &&
                                (!currentVoxels[(uVar8 | 6) + someSideIndex]) &&
                               (!neighborVoxels[(sideIndexInverted1 | 6) + someIndex])) ||
                              (currentVoxelMeshIndex[uVar8 | 7] == neighborMeshIndex &&
                                (neighborVoxelMeshIndex[sideIndexInverted1 | 7] == neighborMeshIndex) &&
                               !currentVoxels[(uVar8 | 7) + someSideIndex] &&
                                (!neighborVoxels[(sideIndexInverted1 | 7) + someIndex]))
                           )
                        {
                            Debug.Assert(gluesA);
                            return true;
                        }
                    }
                }
            }
            else
            {
                bool bVar1 = sideIndex != 0;

                int iVar1 = 7;
                if (bVar1)
                {
                    iVar1 = 0;
                }

                int iVar2 = 0;
                if (bVar1)
                {
                    iVar2 = 7;
                }

                var currentVoxelMeshIndex = currentMeshInfo.VoxelMeshIndex;
                var neighborVoxelMeshIndex = neighborMeshInfo.VoxelMeshIndex;

                for (int index1 = 0, index2 = 0; index1 < 8; index1++, index2 += 64)
                {
                    for (int index3 = 0; index3 < 64; index3 += 8)
                    {
                        int uVar2 = index2 + index3 | iVar1;

                        if (currentVoxelMeshIndex[uVar2] == currentMeshIndex)
                        {
                            int uVar4 = index2 + index3 | iVar2;

                            if (neighborVoxelMeshIndex[uVar4] == neighborMeshIndex &&
                                (!currentVoxels[(sideIndex * 512) + uVar2]) &&
                                (!neighborVoxels[(sideIndexInverted1 * 512) + uVar4]))
                            {
                                Debug.Assert(gluesA);
                                return true;
                            }
                        }
                    }
                }
            }

            Debug.Assert(!gluesA);
            return false;
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
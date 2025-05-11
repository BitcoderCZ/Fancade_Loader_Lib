using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System.Collections;
using System.Diagnostics;

namespace FancadeLoaderLib.Runtime.Bullet;

public readonly struct BlockMesh
{
    public static readonly BlockMesh Empty = new BlockMesh(0, new Array3D<ushort>(int3.Zero), []);

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

    private BlockMesh(int meshCount, Array3D<ushort> blockMeshIdOffsets, short[] blockMeshIds)
    {
        MeshCount = meshCount;
        BlockMeshIdOffsets = blockMeshIdOffsets;
        _blockMeshIds = blockMeshIds;
    }

    public int MeshCount { get; }

    public Array3D<ushort> BlockMeshIdOffsets { get; }

    public ReadOnlySpan<short> BlockMeshIds => _blockMeshIds;

    public static BlockMesh Create(BlockData blocks, PrefabList prefabs, PrefabSegmentMeshes[] segmentMeshes)
    {
        if (blocks.Size == int3.Zero)
        {
            return Empty;
        }

        BitArray?[] attribsCache = new BitArray[RawGame.CurrentNumbStockPrefabs + prefabs.SegmentCount];

        var blocksSize = blocks.Array.Size;
        int blocksLength = blocksSize.X * blocksSize.Y * blocksSize.Z;

        int totalSegmentMeshCount = 0;

        Array3D<ushort> blockMeshIdOffsets = new Array3D<ushort>(blocksSize);

        ushort[] blocksArray = blocks.Array.Array;
        for (int i = 0; i < blocksLength; i++)
        {
            blockMeshIdOffsets[i] = (ushort)totalSegmentMeshCount;

            totalSegmentMeshCount += segmentMeshes[blocksArray[i]].MeshCount;
        }

        var stockPrefabs = StockBlocks.PrefabList;

        short[] blockMeshIds = new short[totalSegmentMeshCount];

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

            int iVar21 = blocksSize.X * blocksSize.Y;
            short sVar12 = 0;
            if (iVar21 != 0)
            {
                sVar12 = (short)(blockIndex / iVar21);
            }

            iVar21 = blockIndex - sVar12 * iVar21;

            short sVar13 = 0;
            if (blocksSize.X != 0)
            {
                sVar13 = (short)(iVar21 / blocksSize.X);
            }

            short sVar5 = (short)(iVar21 - sVar13 * blocksSize.X);

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

                    Debug.Assert(stack.Count == 0);

                    stack.Push((new int3(sVar5, sVar13, sVar12), (short)segmentMeshIndex));

                    while (stack.TryPop(out var item))
                    {
                        var currentPos = item.Pos;

                        int currentBLockIndex = currentPos.X + (currentPos.Y + currentPos.Z * blocksSize.Y) * blocksSize.X;

                        ushort currentBlockId = blocksArray[currentBLockIndex];

                        blockMeshIds[blockMeshIdOffsets[currentBLockIndex] + item.MeshIndex] = meshIndex;

                        for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                        {
                            int3 neighborPos = currentPos + NeighborOffsets[sideIndex];

                            if (!neighborPos.InBounds(blocksSize.X, blocksSize.Y, blocksSize.Z))
                            {
                                continue;
                            }

                            int neighborIndex = neighborPos.X + (neighborPos.Y + neighborPos.Z * blocksSize.Y) * blocksSize.X;

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
                                    Glues(currentBlockId, item.MeshIndex, sideIndex, neighborId, neighborMeshIndex))
                                {
                                    stack.Push((neighborPos, neighborMeshIndex));
                                }
                            }
                        }
                    }

                    meshIndex++;
                }
            }
        }

        return new BlockMesh(meshIndex, blockMeshIdOffsets, blockMeshIds);

        PrefabSegment GetSegment(ushort id)
        {
            return id < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetSegment(id) : prefabs.GetSegment(id);
        }

        Prefab GetPrefab(ushort prefabId)
        {
            return prefabId < RawGame.CurrentNumbStockPrefabs ? stockPrefabs.GetPrefab(prefabId) : prefabs.GetPrefab(prefabId);
        }

        unsafe BitArray GetAttribs(ushort id)
        {
            var attribs = attribsCache[id];

            return attribs is not null
                ? attribs
                : (attribsCache[id] = GetSegment(id).VoxelAttribs ?? new BitArray(8 * 8 * 8 * 6));
        }

        bool Glues(ushort currentBlockId, short currentMeshIndex, int sideIndex, ushort neighborBLockId, int neighborMeshIndex)
        {
            if (sideIndex >= 6)
            {
                return false;
            }

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
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + -2] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 1] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 1] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 1]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + -1] == currentMeshIndex) &&
                             (neighborVoxelMeshIndex[someIndex + uVar8 + 2] == neighborMeshIndex)
                            && (!currentVoxels[someIndex + someSideIndex + 2]) &&
                           (!neighborVoxels[someIndex + sideIndexInverted1 + 2]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 3] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 3] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 3]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 1] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 4] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 4] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 4]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 2] == currentMeshIndex) &&
                             (neighborVoxelMeshIndex[someIndex + uVar8 + 5] == neighborMeshIndex)
                            && (!currentVoxels[someIndex + someSideIndex + 5]) &&
                           (!neighborVoxels[someIndex + sideIndexInverted1 + 5]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 3] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 6] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 6] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 6]))
                        {
                            return true;
                        }

                        if ((currentVoxelMeshIndex[someIndex + lVar5 + 4] == currentMeshIndex) &&
                            (neighborVoxelMeshIndex[someIndex + uVar8 + 7] == neighborMeshIndex) &&
                           !currentVoxels[someIndex + someSideIndex + 7] &&
                            (!neighborVoxels[someIndex + sideIndexInverted1 + 7]))
                        {
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
                             currentVoxelMeshIndex[uVar8 | 2] == neighborMeshIndex &&
                               (neighborVoxelMeshIndex[sideIndexInverted1 | 2] == neighborMeshIndex) &&
                              !currentVoxels[(uVar8 | 2) + someSideIndex] &&
                               (!neighborVoxels[(sideIndexInverted1 | 2) + someIndex]) ||
                            (currentVoxelMeshIndex[uVar8 | 3] == neighborMeshIndex &&
                                  (neighborVoxelMeshIndex[sideIndexInverted1 | 3] == neighborMeshIndex) &&
                                 (!currentVoxels[(uVar8 | 3) + someSideIndex]) &&
                                (!neighborVoxels[(sideIndexInverted1 | 3) + someIndex])) ||
                               currentVoxelMeshIndex[uVar8 | 4] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 4] == neighborMeshIndex) &&
                                !currentVoxels[(uVar8 | 4) + someSideIndex] &&
                                 (!neighborVoxels[(sideIndexInverted1 | 4) + someIndex]) ||
                              currentVoxelMeshIndex[uVar8 | 5] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 5] == neighborMeshIndex) &&
                                (!currentVoxels[(uVar8 | 5) + someSideIndex]) &&
                               (!neighborVoxels[(sideIndexInverted1 | 5) + someIndex]) ||
                             (currentVoxelMeshIndex[uVar8 | 6] == neighborMeshIndex &&
                                 (neighborVoxelMeshIndex[sideIndexInverted1 | 6] == neighborMeshIndex) &&
                                (!currentVoxels[(uVar8 | 6) + someSideIndex]) &&
                               (!neighborVoxels[(sideIndexInverted1 | 6) + someIndex])) ||
                              currentVoxelMeshIndex[uVar8 | 7] == neighborMeshIndex &&
                                (neighborVoxelMeshIndex[sideIndexInverted1 | 7] == neighborMeshIndex) &&
                               !currentVoxels[(uVar8 | 7) + someSideIndex] &&
                                (!neighborVoxels[(sideIndexInverted1 | 7) + someIndex])
                           )
                        {
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
                                (!currentVoxels[sideIndex * 512 + uVar2]) &&
                                (!neighborVoxels[sideIndexInverted1 * 512 + uVar4]))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}

using MathUtils.Vectors;
using System.Diagnostics;
using System.Numerics;

namespace FancadeLoaderLib.Runtime.Bullet;

public sealed class PrefabSegmentMeshes
{
    private static readonly Vector3[] chunk_voxels_vertex_pos0 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos1 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos2 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos3 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos0_2 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos1_2 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos2_2 = new Vector3[6 * 0x30000];
    private static readonly Vector3[] chunk_voxels_vertex_pos3_2 = new Vector3[6 * 0x30000];

    private static readonly Vector2[] chunk_voxels_uv0 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv1 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv2 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv3 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv0_2 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv1_2 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv2_2 = new Vector2[6 * 0x30000];
    private static readonly Vector2[] chunk_voxels_uv3_2 = new Vector2[6 * 0x30000];

    private static readonly byte[] EmptyVoxelMeshIndex = new byte[8 * 8 * 8];

    public static readonly PrefabSegmentMeshes Empty = new PrefabSegmentMeshes(0, EmptyVoxelMeshIndex, [], int3.Zero, int3.Zero);

    private static readonly short3[] NeighborOffsets =
    [
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    ];

    private readonly byte[] _voxelMeshIndex;

    private readonly PrefabSegmentMesh[] _meshes;

    private PrefabSegmentMeshes(int meshCount, byte[] voxelMeshIndex, PrefabSegmentMesh[] meshes, int3 minPosition, int3 maxPosition)
    {
        Debug.Assert(voxelMeshIndex.Length == 8 * 8 * 8);

        MeshCount = meshCount;
        _voxelMeshIndex = voxelMeshIndex;
        _meshes = meshes;
        MinPosition = minPosition;
        MaxPosition = maxPosition;
    }

    public int MeshCount { get; }

    public int3 MinPosition { get; }

    public int3 MaxPosition { get; }

    public ReadOnlySpan<byte> VoxelMeshIndex => _voxelMeshIndex;

    public ReadOnlySpan<PrefabSegmentMesh> Meshes => _meshes;

#if NET8_0_OR_GREATER
    public static PrefabSegmentMeshes Create(PrefabSegment segment)
#else
    public static unsafe PrefabSegmentMeshes Create(PrefabSegment segment)
#endif
    {
        if (segment.Voxels is null || segment.PrefabId == 0)
        {
            return Empty;
        }

        byte[] voxelMeshIndex = new byte[8 * 8 * 8];
        byte meshCount = 0;

        voxelMeshIndex.AsSpan().Fill(byte.MaxValue);

        Stack<int3>? stack = null;

        int voxelIndex = 0;

        var voxels = segment.Voxels;

        int3 min = new int3(int.MaxValue, int.MaxValue, int.MaxValue);
        int3 max = new int3(int.MinValue, int.MinValue, int.MinValue);

        for (int z = 0; z < 8; z++)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++, voxelIndex++)
                {
                    if (voxels[voxelIndex].IsEmpty || voxelMeshIndex[voxelIndex] != byte.MaxValue)
                    {
                        continue;
                    }

                    stack ??= new Stack<int3>();

                    stack.Push(new int3(x, y, z));

                    while (stack.TryPop(out var currentPos))
                    {
                        min = int3.Min(min, currentPos);
                        max = int3.Max(max, currentPos);

                        int currentVoxelIndex = PrefabSegment.IndexVoxels(currentPos);

                        voxelMeshIndex[currentVoxelIndex] = meshCount;

                        var voxel = voxels[currentVoxelIndex];

                        for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                        {
                            int3 neighborPos = currentPos + NeighborOffsets[sideIndex];

                            if (voxel.Attribs[sideIndex] || !neighborPos.InBounds(8, 8, 8))
                            {
                                continue;
                            }

                            int neighborVoxelIndex = PrefabSegment.IndexVoxels(neighborPos);

                            var neighbor = voxels[neighborVoxelIndex];

                            if (neighbor.IsEmpty || voxelMeshIndex[neighborVoxelIndex] != byte.MaxValue || neighbor.Attribs[sideIndex ^ 1])
                            {
                                continue;
                            }

                            stack.Push(neighborPos);
                        }
                    }

                    meshCount++;
                }
            }
        }

        return new PrefabSegmentMeshes(meshCount, voxelMeshIndex, ChunkVoxels(meshCount, voxelMeshIndex, voxels), min, max);
    }

    private static unsafe PrefabSegmentMesh[] ChunkVoxels(int meshCount, byte[] voxelMeshIndex, Voxel[] voxels)
    {
        Debug.Assert(voxels.Length == 8 * 8 * 8);

        PrefabSegmentMesh[] meshes = new PrefabSegmentMesh[meshCount];

        Span<byte> currentMeshVoxels = stackalloc byte[8 * 8 * 8 * 6];
        Span<byte> buffer64 = stackalloc byte[64];
        byte[] some_buffer_for_tex_optimization = new byte[10240];

        Span<byte> rawVoxels = stackalloc byte[8 * 8 * 8 * 6];
        Voxel.ToRaw(voxels, rawVoxels);

        Span<ulong> mesh_bitfield = stackalloc ulong[6];
        Span<int> mesh_lengths0 = stackalloc int[6];
        Span<int> mesh_lengths1 = stackalloc int[6];
        Span<Vector3[]> mesh_verts0 = new Vector3[6][];
        Span<Vector3[]> mesh_verts1 = new Vector3[6][];
        Span<Vector3[]> mesh_verts2 = new Vector3[6][];
        Span<Vector3[]> mesh_verts3 = new Vector3[6][];
        Span<Vector2[]> mesh_uvs0 = new Vector2[6][];
        Span<Vector2[]> mesh_uvs1 = new Vector2[6][];
        Span<Vector2[]> mesh_uvs2 = new Vector2[6][];
        Span<Vector2[]> mesh_uvs3 = new Vector2[6][];

        short3 voxelsMin = new short3(short.MaxValue, short.MaxValue, short.MaxValue);
        short3 voxelsMax = new short3(short.MinValue, short.MinValue, short.MinValue);

        int[] some_array = [1, 1, 2, 2, 1, 1];
        int[] vector_axis_array = [2, 2, 0, 0, 0, 0];

        int posAxisIndex = 1;
        int pos2AxisIndex = 1;

        int counter0 = 1;

        for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
        {
            int voxelIndex;
            //for (voxelIndex = 0; voxelIndex < 8 * 8 * 8; voxelIndex++)
            //{
            //    if (voxelMeshIndex[voxelIndex] == meshIndex)
            //    {
            //        currentMeshVoxels[voxelIndex + 512 * 0] = voxels[voxelIndex].Colors[0];
            //        currentMeshVoxels[voxelIndex + 512 * 1] = voxels[voxelIndex].Colors[1];
            //        currentMeshVoxels[voxelIndex + 512 * 2] = voxels[voxelIndex].Colors[2];
            //        currentMeshVoxels[voxelIndex + 512 * 3] = voxels[voxelIndex].Colors[3];
            //        currentMeshVoxels[voxelIndex + 512 * 4] = voxels[voxelIndex].Colors[4];
            //        currentMeshVoxels[voxelIndex + 512 * 5] = voxels[voxelIndex].Colors[5];
            //    }
            //}

            //voxelIndex = 0;

            //for (int sideIndex = 0; sideIndex < 6; sideIndex++)
            //{
            //    int oppositeSideIndex = sideIndex ^ 1;

            //    for (int z = 0; z < 8; z++)
            //    {
            //        for (int y = 0; y < 8; y++)
            //        {
            //            if (currentMeshVoxels[voxelIndex] != 0)
            //            {
            //                var currentPos = new short3(0, y, z);

            //                voxelsMin = short3.Min(voxelsMin, currentPos);
            //                voxelsMax = short3.Max(voxelsMax, currentPos);

            //                int neighborVoxelIndex;
            //                switch (sideIndex)
            //                {
            //                    case 0:
            //                        neighborVoxelIndex = y * 8 + z * 64 + 1;

            //                        break;
            //                    case 2:
            //                        if (y >= 7)
            //                        {
            //                            neighborVoxelIndex = -1;
            //                        }
            //                        else
            //                        {
            //                            neighborVoxelIndex = y * 8 + z * 64 + 8;
            //                        }

            //                        break;
            //                    case 3:
            //                        if (y == 0)
            //                        {
            //                            neighborVoxelIndex = -1;
            //                        }
            //                        else
            //                        {
            //                            neighborVoxelIndex = y * 8 + z * 64 - 8;
            //                        }

            //                        break;
            //                    case 4:
            //                        if (z >= 7)
            //                        {
            //                            neighborVoxelIndex = -1;
            //                        }
            //                        else
            //                        {
            //                            neighborVoxelIndex = y * 8 + z * 64 + 64;
            //                        }

            //                        break;
            //                    case 5:
            //                        if (z == 0)
            //                        {
            //                            neighborVoxelIndex = -1;
            //                        }
            //                        else
            //                        {
            //                            neighborVoxelIndex = y * 8 + z * 64 - 64;
            //                        }

            //                        break;
            //                    default:
            //                        goto skipAssign;
            //                }

            //                if (neighborVoxelIndex != -1 && currentMeshVoxels[neighborVoxelIndex + oppositeSideIndex * 512] != 0)
            //                {
            //                    currentMeshVoxels[voxelIndex] = 0;
            //                    currentMeshVoxels[neighborVoxelIndex + oppositeSideIndex * 512] = 0;
            //                }

            //            skipAssign: { }
            //            }

            //            voxelIndex++;

            //            for (int x = 1; x < 7; x++)
            //            {
            //                if (currentMeshVoxels[voxelIndex] != 0)
            //                {
            //                    var currentPos = new short3(x, y, z);

            //                    voxelsMin = short3.Min(voxelsMin, currentPos);
            //                    voxelsMax = short3.Max(voxelsMax, currentPos);

            //                    int neighborVoxelIndex;
            //                    switch (sideIndex)
            //                    {
            //                        case 0:
            //                            if (x >= 7)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x + 1;
            //                            }

            //                            break;
            //                        case 1:
            //                            if (y == 0)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x - 1;
            //                            }

            //                            break;
            //                        case 2:
            //                            if (y >= 7)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x + 8;
            //                            }

            //                            break;
            //                        case 3:
            //                            if (y == 0)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x - 8;
            //                            }

            //                            break;
            //                        case 4:
            //                            if (z >= 7)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x + 64;
            //                            }

            //                            break;
            //                        default:
            //                            Debug.Assert(sideIndex == 5);
            //                            if (z == 0)
            //                            {
            //                                neighborVoxelIndex = -1;
            //                            }
            //                            else
            //                            {
            //                                neighborVoxelIndex = y * 8 + z * 64 + x - 64;
            //                            }

            //                            break;
            //                    }

            //                    if (neighborVoxelIndex != -1 && currentMeshVoxels[neighborVoxelIndex + oppositeSideIndex * 512] != 0)
            //                    {
            //                        currentMeshVoxels[voxelIndex] = 0;
            //                        currentMeshVoxels[neighborVoxelIndex + oppositeSideIndex * 512] = 0;
            //                    }
            //                }

            //                voxelIndex++;
            //            }
            //        }
            //    }
            //}

            //voxelIndex = 0;
            //for (int sideIndex = 0; sideIndex < 6; sideIndex++)
            //{
            //    mesh_lengths0[sideIndex] = 0;
            //    mesh_lengths1[sideIndex] = 0;

            //    for (int z = 0; z < 8; z++)
            //    {
            //        for (int y = 0; y < 8; y++)
            //        {
            //            for (int x = 0; x < 8; x++, voxelIndex++)
            //            {
            //                byte currentVoxel = currentMeshVoxels[voxelIndex];

            //                if (currentVoxel == 0)
            //                {
            //                    continue;
            //                }

            //                if (sideIndex < 6)
            //                {
            //                    pos2AxisIndex = some_array[sideIndex];
            //                    posAxisIndex = vector_axis_array[sideIndex];
            //                }

            //                buffer64.Clear();
            //                buffer64[0] = currentVoxel;

            //                short3 pos = new short3(x, y, z);

            //                short posSelectedAxisValue = pos[posAxisIndex];

            //                int buffer64Count;
            //                int idk;
            //                if (posSelectedAxisValue < 7)
            //                {
            //                    idk = Math.Max((int)posSelectedAxisValue, 6);

            //                    buffer64Count = 1;

            //                    do
            //                    {
            //                        posSelectedAxisValue++;
            //                        pos[posAxisIndex] = posSelectedAxisValue;

            //                        byte voxel = currentMeshVoxels[pos.X + pos.Y * 8 + pos.Z * 64 + sideIndex * 512];

            //                        buffer64[buffer64Count] = voxel;
            //                        if (voxel == 0)
            //                        {
            //                            break;
            //                        }

            //                        buffer64Count++;
            //                    } while ((idk - posSelectedAxisValue) + 2 != buffer64Count);
            //                }
            //                else
            //                {
            //                    buffer64Count = 1;
            //                }

            //                //llong xyzIndexPacked = ((z << 32) & (short.MaxValue << 32)) | ((y << 16) & (short.MaxValue << 16)) | (x & short.MaxValue);

            //                var pos2 = new int3(x, y, z);
            //                int selectedLoopIndexValue = pos2[pos2AxisIndex];

            //                int index;
            //                if (selectedLoopIndexValue < 7)
            //                {
            //                    idk = (Math.Max(selectedLoopIndexValue, 6) - selectedLoopIndexValue) + 2;

            //                    index = 1;
            //                    int buffer64Index = 0;
            //                    do
            //                    {
            //                        buffer64Index += 8;

            //                        selectedLoopIndexValue++;
            //                        pos2[pos2AxisIndex] = selectedLoopIndexValue;

            //                        int3 pos2Copy = pos2;
            //                        for (int i = 0; i < buffer64Count; i++)
            //                        {
            //                            byte voxel = currentMeshVoxels[pos2Copy.X + pos2Copy.Y * 8 + pos2Copy.Z * 64 + sideIndex * 512];
            //                            buffer64[buffer64Index + i] = voxel;

            //                            if (voxel == 0)
            //                            {
            //                                idk = index;
            //                                goto loop_break;
            //                            }

            //                            pos2Copy[posAxisIndex]++;
            //                        }

            //                        pos2Copy = pos2;
            //                        for (int i = 0; i < buffer64Count; i++)
            //                        {
            //                            currentMeshVoxels[pos2Copy.X + pos2Copy.Y * 8 + pos2Copy.Z * 64 + sideIndex * 512] = 0;

            //                            pos2Copy[posAxisIndex]++;
            //                        }

            //                    } while (idk != index);
            //                }
            //                else
            //                {
            //                    idk = 1;
            //                }

            //            loop_break:
            //                index = -1;
            //                int iVar39 = counter0 * 10 - 10;

            //                bool condition;
            //                do
            //                {
            //                    int iVar32 = buffer64Count <= idk ? buffer64Count - 1 : idk;

            //                    int iVar12 = idk >= 0 ? iVar32 : 0;

            //                    ulong idk2 = 0xffffffffffffffff;
            //                    int index2 = 0;
            //                    do
            //                    {
            //                        iVar32 = (int)idk2;
            //                        if (idk <= (long)idk2)
            //                        {
            //                            iVar32 = idk + 0x1fffffff;
            //                        }

            //                        int iVar10 = 0;

            //                        if (-1 < (long)idk2)
            //                        {
            //                            iVar10 = iVar32 << 3;
            //                        }

            //                        idk2 = idk2 + 1;

            //                        some_buffer_for_tex_optimization[iVar39 + index2] = buffer64[iVar10 + iVar12];
            //                        index2 += 10;
            //                    } while ((ulong)(idk + 1) != idk2);

            //                    condition = index != buffer64Count;

            //                    index++;
            //                } while (condition);

            //                float someVecX0 = default;
            //                float someVecX1 = default;
            //                float someVecY0 = default;
            //                float someVecY1 = default;
            //                float someVecZ0 = default;
            //                float someVecZ1 = default;
            //                float someVecZ2 = default;
            //                float someVecZ3 = default;

            //                float uvX0 = default;
            //                float uvX1 = default;
            //                float uvY0 = default;
            //                float uvY1 = default;

            //                if (sideIndex >= 6)
            //                {
            //                    goto assign0;
            //                }

            //                const float UVScale = 1f / 2048f;
            //                switch (sideIndex)
            //                {
            //                    case 0:
            //                        someVecY0 = idk + y;
            //                        someVecX0 = x + 1.0f;
            //                        someVecZ0 = buffer64Count + z;
            //                        uvX1 = UVScale;
            //                        uvX0 = (buffer64Count + 1) * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        uvY1 = counter0 * UVScale;
            //                        someVecX1 = someVecX0;
            //                        someVecZ3 = someVecZ0;
            //                        someVecZ1 = z;
            //                        if (x != 7)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                    case 1:
            //                        someVecY0 = idk + y;
            //                        someVecX0 = x;
            //                        someVecZ1 = buffer64Count + z;
            //                        uvX0 = UVScale;
            //                        uvX1 = (buffer64Count + 1) * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        uvY1 = counter0 * UVScale;
            //                        someVecX1 = someVecX0;
            //                        someVecZ2 = someVecZ1;
            //                        if (x != 0)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                    case 2:
            //                        someVecZ0 = idk + z;
            //                        uvX1 = UVScale;
            //                        uvX0 = (buffer64Count + 1) * UVScale;
            //                        someVecY1 = y + 1.0f;
            //                        someVecY0 = y + 1.0f;
            //                        uvY1 = counter0 * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        someVecX0 = buffer64Count + x;
            //                        someVecX1 = x;
            //                        someVecZ1 = someVecZ0;
            //                        someVecZ2 = someVecZ3;
            //                        if (y != 7)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                    case 3:
            //                        someVecZ0 = idk + z;
            //                        uvX0 = UVScale;
            //                        uvX1 = (buffer64Count + 1) * UVScale;
            //                        someVecY0 = y;

            //                        uvY1 = counter0 * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        someVecX0 = x;
            //                        someVecX1 = buffer64Count + x;
            //                        someVecZ1 = someVecZ0;
            //                        if (y != 0)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                    case 4:
            //                        uvX0 = UVScale;
            //                        uvX1 = (buffer64Count + 1) * UVScale;
            //                        someVecZ3 = z + 1.0f;
            //                        someVecZ0 = z + 1.0f;
            //                        someVecY0 = idk + y;
            //                        uvY1 = counter0 * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        someVecX0 = x;
            //                        someVecX1 = buffer64Count + x;
            //                        someVecZ1 = someVecZ0;
            //                        someVecZ2 = someVecZ3;
            //                        if (z != 7)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                    case 5:
            //                        uvX1 = UVScale;
            //                        uvX0 = (buffer64Count + 1) * UVScale;
            //                        someVecY0 = x + y;
            //                        uvY1 = counter0 * UVScale;
            //                        uvY0 = (idk + counter0) * UVScale;
            //                        someVecX0 = buffer64Count + x;
            //                        someVecX1 = x;
            //                        someVecZ1 = someVecZ0;
            //                        if (z != 0)
            //                        {
            //                            goto assign0;
            //                        }

            //                        goto assign1;
            //                }

            //            assign0:
            //                index = mesh_lengths0[sideIndex];

            //                chunk_voxels_vertex_pos0[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX0, someVecY0, someVecZ0) * 0.125f;
            //                chunk_voxels_vertex_pos1[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX1, someVecY0, someVecZ1) * 0.125f;
            //                chunk_voxels_vertex_pos2[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX1, someVecY1, someVecZ2) * 0.125f;
            //                chunk_voxels_vertex_pos3[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX0, someVecY1, someVecZ3) * 0.125f;

            //                chunk_voxels_uv0[sideIndex * 0x30000 + (long)index] = new Vector2(uvX0, uvY0);
            //                chunk_voxels_uv1[sideIndex * 0x30000 + (long)index] = new Vector2(uvX1, uvY0);
            //                chunk_voxels_uv2[sideIndex * 0x30000 + (long)index] = new Vector2(uvX1, uvY1);
            //                chunk_voxels_uv3[sideIndex * 0x30000 + (long)index] = new Vector2(uvX0, uvY1);

            //                mesh_lengths0[sideIndex]++;

            //                goto skipAssign1;

            //            assign1:
            //                index = mesh_lengths1[sideIndex];

            //                chunk_voxels_vertex_pos0_2[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX0, someVecY0, someVecZ0) * 0.125f;
            //                chunk_voxels_vertex_pos1_2[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX1, someVecY0, someVecZ1) * 0.125f;
            //                chunk_voxels_vertex_pos2_2[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX1, someVecY1, someVecZ2) * 0.125f;
            //                chunk_voxels_vertex_pos3_2[sideIndex * 0x30000 + (long)index] = new Vector3(someVecX0, someVecY1, someVecZ3) * 0.125f;

            //                chunk_voxels_uv0_2[sideIndex * 0x30000 + (long)index] = new Vector2(uvX0, uvY0);
            //                chunk_voxels_uv1_2[sideIndex * 0x30000 + (long)index] = new Vector2(uvX1, uvY0);
            //                chunk_voxels_uv2_2[sideIndex * 0x30000 + (long)index] = new Vector2(uvX1, uvY1);
            //                chunk_voxels_uv3_2[sideIndex * 0x30000 + (long)index] = new Vector2(uvX0, uvY1);

            //                mesh_lengths1[sideIndex]++;

            //            skipAssign1:
            //                counter0 += idk + 2;

            //                if (1014 < counter0)
            //                {
            //                    throw new TooComplexGeometryException();
            //                    //counter0 = 1;
            //                }
            //            }
            //        }
            //    }
            //}

            currentMeshVoxels.Clear();

            mesh_bitfield.Clear();

            ulong value = 0;
            var rawVoxelsSlice = rawVoxels[7..];
            voxelIndex = 7;
            do
            {
                if (rawVoxelsSlice[0] != 0)
                {
                    value |= 1UL << (voxelIndex - 7 & 0b0011_1111);
                }

                if (rawVoxelsSlice[8] != 0)
                {
                    value |= 1UL << (voxelIndex - 6 & 0b0011_1111);
                }

                if (rawVoxelsSlice[16] != 0)
                {
                    value |= 1UL << (voxelIndex - 5 & 0b0011_1111);
                }

                if (rawVoxelsSlice[24] != 0)
                {
                    value |= 1UL << (voxelIndex - 4 & 0b0011_1111);
                }

                if (rawVoxelsSlice[32] != 0)
                {
                    value |= 1UL << (voxelIndex - 3 & 0b0011_1111);
                }

                if (rawVoxelsSlice[40] != 0)
                {
                    value |= 1UL << (voxelIndex - 2 & 0b0011_1111);
                }

                if (rawVoxelsSlice[48] != 0)
                {
                    value |= 1UL << (voxelIndex - 1 & 0b0011_1111);
                }

                if (rawVoxelsSlice[56] != 0)
                {
                    value |= 1UL << (voxelIndex - 0 & 0b0011_1111);
                }

                voxelIndex += 8;
                rawVoxelsSlice = rawVoxelsSlice[64..];
            } while (voxelIndex != 71);

            mesh_bitfield[0] = value;

            value = 0;
            rawVoxelsSlice = currentMeshVoxels[512..];
            voxelIndex = 7;
            do
            {
                if (rawVoxelsSlice[0] != 0)
                {
                    value |= 1UL << (voxelIndex - 7 & 0b0011_1111);
                }

                if (rawVoxelsSlice[8] != 0)
                {
                    value |= 1UL << (voxelIndex - 6 & 0b0011_1111);
                }

                if (rawVoxelsSlice[16] != 0)
                {
                    value |= 1UL << (voxelIndex - 5 & 0b0011_1111);
                }

                if (rawVoxelsSlice[24] != 0)
                {
                    value |= 1UL << (voxelIndex - 4 & 0b0011_1111);
                }

                if (rawVoxelsSlice[32] != 0)
                {
                    value |= 1UL << (voxelIndex - 3 & 0b0011_1111);
                }

                if (rawVoxelsSlice[40] != 0)
                {
                    value |= 1UL << (voxelIndex - 2 & 0b0011_1111);
                }

                if (rawVoxelsSlice[48] != 0)
                {
                    value |= 1UL << (voxelIndex - 1 & 0b0011_1111);
                }

                if (rawVoxelsSlice[56] != 0)
                {
                    value |= 1UL << (voxelIndex - 0 & 0b0011_1111);
                }

                voxelIndex += 8;
                rawVoxelsSlice = rawVoxelsSlice[64..];
            } while (voxelIndex != 71);

            mesh_bitfield[1] = value;

            value = 0;
            rawVoxelsSlice = currentMeshVoxels[1080..];
            voxelIndex = 7;
            do
            {
                if (rawVoxelsSlice[0] != 0)
                {
                    value |= 1UL << (voxelIndex - 7 & 0b0011_1111);
                }

                if (rawVoxelsSlice[1] != 0)
                {
                    value |= 1UL << (voxelIndex - 6 & 0b0011_1111);
                }

                if (rawVoxelsSlice[2] != 0)
                {
                    value |= 1UL << (voxelIndex - 5 & 0b0011_1111);
                }

                if (rawVoxelsSlice[3] != 0)
                {
                    value |= 1UL << (voxelIndex - 4 & 0b0011_1111);
                }

                if (rawVoxelsSlice[4] != 0)
                {
                    value |= 1UL << (voxelIndex - 3 & 0b0011_1111);
                }

                if (rawVoxelsSlice[5] != 0)
                {
                    value |= 1UL << (voxelIndex - 2 & 0b0011_1111);
                }

                if (rawVoxelsSlice[6] != 0)
                {
                    value |= 1UL << (voxelIndex - 1 & 0b0011_1111);
                }

                if (rawVoxelsSlice[7] != 0)
                {
                    value |= 1UL << (voxelIndex - 0 & 0b0011_1111);
                }

                voxelIndex += 8;
                rawVoxelsSlice = rawVoxelsSlice[64..];
            } while (voxelIndex != 71);

            mesh_bitfield[2] = value;

            value = 0;
            rawVoxelsSlice = currentMeshVoxels[1536..];
            voxelIndex = 7;
            do
            {
                if (rawVoxelsSlice[0] != 0)
                {
                    value |= 1UL << (voxelIndex - 7 & 0b0011_1111);
                }

                if (rawVoxelsSlice[1] != 0)
                {
                    value |= 1UL << (voxelIndex - 6 & 0b0011_1111);
                }

                if (rawVoxelsSlice[2] != 0)
                {
                    value |= 1UL << (voxelIndex - 5 & 0b0011_1111);
                }

                if (rawVoxelsSlice[3] != 0)
                {
                    value |= 1UL << (voxelIndex - 4 & 0b0011_1111);
                }

                if (rawVoxelsSlice[4] != 0)
                {
                    value |= 1UL << (voxelIndex - 3 & 0b0011_1111);
                }

                if (rawVoxelsSlice[5] != 0)
                {
                    value |= 1UL << (voxelIndex - 2 & 0b0011_1111);
                }

                if (rawVoxelsSlice[6] != 0)
                {
                    value |= 1UL << (voxelIndex - 1 & 0b0011_1111);
                }

                if (rawVoxelsSlice[7] != 0)
                {
                    value |= 1UL << (voxelIndex - 0 & 0b0011_1111);
                }

                voxelIndex += 8;
                rawVoxelsSlice = rawVoxelsSlice[64..];
            } while (voxelIndex != 71);

            mesh_bitfield[3] = value;

            value = 0;
            voxelIndex = 0;

            do
            {
                if (rawVoxels[voxelIndex + 2496] != 0)
                {
                    value |= 1UL << (voxelIndex + 0 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2497] != 0)
                {
                    value |= 1UL << (voxelIndex + 1 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2498] != 0)
                {
                    value |= 1UL << (voxelIndex + 2 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2499] != 0)
                {
                    value |= 1UL << (voxelIndex + 3 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2500] != 0)
                {
                    value |= 1UL << (voxelIndex + 4 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2501] != 0)
                {
                    value |= 1UL << (voxelIndex + 5 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2502] != 0)
                {
                    value |= 1UL << (voxelIndex + 6 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2503] != 0)
                {
                    value |= 1UL << (voxelIndex + 7 & 0b0011_1111);
                }

                voxelIndex += 8;
            } while (voxelIndex != 64);

            mesh_bitfield[4] = value;

            value = 0;
            voxelIndex = 0;

            do
            {
                if (rawVoxels[voxelIndex + 2560] != 0)
                {
                    value |= 1UL << (voxelIndex + 0 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2561] != 0)
                {
                    value |= 1UL << (voxelIndex + 1 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2562] != 0)
                {
                    value |= 1UL << (voxelIndex + 2 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2563] != 0)
                {
                    value |= 1UL << (voxelIndex + 3 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2564] != 0)
                {
                    value |= 1UL << (voxelIndex + 4 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2565] != 0)
                {
                    value |= 1UL << (voxelIndex + 5 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2566] != 0)
                {
                    value |= 1UL << (voxelIndex + 6 & 0b0011_1111);
                }

                if (rawVoxels[voxelIndex + 2567] != 0)
                {
                    value |= 1UL << (voxelIndex + 7 & 0b0011_1111);
                }

                voxelIndex += 8;
            } while (voxelIndex != 64);

            mesh_bitfield[5] = value;

            for (int sideIndex = 0; sideIndex < 6; sideIndex++)
            {
                int length0 = mesh_lengths0[sideIndex];
                int length1 = mesh_lengths1[sideIndex];

                int lengthCombined = length0 + length1;

                mesh_verts0[sideIndex] = new Vector3[lengthCombined];
                mesh_verts1[sideIndex] = new Vector3[lengthCombined];
                mesh_verts2[sideIndex] = new Vector3[lengthCombined];
                mesh_verts3[sideIndex] = new Vector3[lengthCombined];
                mesh_uvs0[sideIndex] = new Vector2[lengthCombined];
                mesh_uvs1[sideIndex] = new Vector2[lengthCombined];
                mesh_uvs2[sideIndex] = new Vector2[lengthCombined];
                mesh_uvs3[sideIndex] = new Vector2[lengthCombined];

                chunk_voxels_vertex_pos0.AsSpan(0, length0).CopyTo(mesh_verts0[sideIndex]);
                chunk_voxels_vertex_pos0_2.AsSpan(0, length1).CopyTo(mesh_verts0[sideIndex][length0..]);
                chunk_voxels_vertex_pos1.AsSpan(0, length0).CopyTo(mesh_verts1[sideIndex]);
                chunk_voxels_vertex_pos1_2.AsSpan(0, length1).CopyTo(mesh_verts1[sideIndex][length0..]);
                chunk_voxels_vertex_pos2.AsSpan(0, length0).CopyTo(mesh_verts2[sideIndex]);
                chunk_voxels_vertex_pos2_2.AsSpan(0, length1).CopyTo(mesh_verts2[sideIndex][length0..]);
                chunk_voxels_vertex_pos3.AsSpan(0, length0).CopyTo(mesh_verts3[sideIndex]);
                chunk_voxels_vertex_pos3_2.AsSpan(0, length1).CopyTo(mesh_verts3[sideIndex][length0..]);

                chunk_voxels_uv0.AsSpan(0, length0).CopyTo(mesh_uvs0[sideIndex]);
                chunk_voxels_uv0_2.AsSpan(0, length1).CopyTo(mesh_uvs0[sideIndex][length0..]);
                chunk_voxels_uv1.AsSpan(0, length0).CopyTo(mesh_uvs1[sideIndex]);
                chunk_voxels_uv1_2.AsSpan(0, length1).CopyTo(mesh_uvs1[sideIndex][length0..]);
                chunk_voxels_uv2.AsSpan(0, length0).CopyTo(mesh_uvs2[sideIndex]);
                chunk_voxels_uv2_2.AsSpan(0, length1).CopyTo(mesh_uvs2[sideIndex][length0..]);
                chunk_voxels_uv3.AsSpan(0, length0).CopyTo(mesh_uvs3[sideIndex]);
                chunk_voxels_uv3_2.AsSpan(0, length1).CopyTo(mesh_uvs3[sideIndex][length0..]);
            }

            int meshVoxelCount = 0;

            for (int i = 0; i < voxels.Length; i++)
            {
                if (!voxels[i].IsEmpty && voxelMeshIndex[i] == meshIndex)
                {
                    meshVoxelCount++;
                }
            }

            meshes[meshIndex] = new PrefabSegmentMesh(Math.Min(meshVoxelCount / 2, 255), mesh_bitfield, mesh_lengths0, mesh_lengths1, mesh_verts0, mesh_verts1, mesh_verts2, mesh_verts3, mesh_uvs0, mesh_uvs1, mesh_uvs2, mesh_uvs3);
        }

        // uvs offset by chunk pos

        return meshes;
    }
}

// <copyright file="PrefabSegmentMeshes.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Diagnostics;

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores the meshes of a <see cref="PrefabSegment"/>.
/// </summary>
public sealed class PrefabSegmentMeshes
{
    /// <summary>
    /// An empty <see cref="PrefabSegmentMeshes"/> instance.
    /// </summary>
    public static readonly PrefabSegmentMeshes Empty = new PrefabSegmentMeshes(0, new byte[8 * 8 * 8], [], int3.Zero, int3.Zero);

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
        Debug.Assert(voxelMeshIndex.Length == 8 * 8 * 8, $"{nameof(voxelMeshIndex)} should be {8 * 8 * 8} elements long.");

        MeshCount = meshCount;
        _voxelMeshIndex = voxelMeshIndex;
        _meshes = meshes;
        MinPosition = minPosition;
        MaxPosition = maxPosition;
    }

    /// <summary>
    /// Gets the number of meshes in the segment.
    /// </summary>
    /// <value>The number of meshes in the segment.</value>
    public int MeshCount { get; }

    /// <summary>
    /// Gets the minimum position of a voxel.
    /// </summary>
    /// <value>Minimum position of a voxel.</value>
    public int3 MinPosition { get; }

    /// <summary>
    /// Gets the maximum position of a voxel.
    /// </summary>
    /// <value>Maximum position of a voxel.</value>
    public int3 MaxPosition { get; }

    /// <summary>
    /// Gets the mesh indices of the voxels.
    /// </summary>
    /// <remarks>Indexing same as <see cref="Voxels"/>.</remarks>
    /// <value>Mesh indices of the voxels.</value>
    public ReadOnlySpan<byte> VoxelMeshIndex => _voxelMeshIndex;

    /// <summary>
    /// Gets the meshes.
    /// </summary>
    /// <value>The meshes.</value>
    public ReadOnlySpan<PrefabSegmentMesh> Meshes => _meshes;

    /// <summary>
    /// Creates a new instance of the <see cref="PrefabSegmentMeshes"/> class.
    /// </summary>
    /// <param name="segment">The <see cref="PrefabSegment"/> to create the <see cref="PrefabSegmentMeshes"/> for.</param>
    /// <returns>The created <see cref="PrefabSegmentMeshes"/>.</returns>
    public static PrefabSegmentMeshes Create(PrefabSegment segment)
    {
        if (segment.Voxels.IsEmpty || segment.PrefabId == 0)
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
                    if (voxels.GetRawFace(voxelIndex) == 0 || voxelMeshIndex[voxelIndex] != byte.MaxValue)
                    {
                        continue;
                    }

                    stack ??= new Stack<int3>();

                    stack.Push(new int3(x, y, z));

                    while (stack.TryPop(out var currentPos))
                    {
                        min = int3.Min(min, currentPos);
                        max = int3.Max(max, currentPos);

                        int currentVoxelIndex = Voxels.Index(currentPos, 0);

                        voxelMeshIndex[currentVoxelIndex] = meshCount;

                        var voxel = voxels[currentPos];

                        for (int sideIndex = 0; sideIndex < 6; sideIndex++)
                        {
                            int3 neighborPos = currentPos + NeighborOffsets[sideIndex];

                            unsafe
                            {
                                if (voxel.Attribs[sideIndex] || !neighborPos.InBounds(8, 8, 8))
                                {
                                    continue;
                                }

                                int neighborVoxelIndex = Voxels.Index(neighborPos, 0);

                                var neighbor = voxels[neighborPos];

                                if (neighbor.IsEmpty || voxelMeshIndex[neighborVoxelIndex] != byte.MaxValue || neighbor.Attribs[sideIndex ^ 1])
                                {
                                    continue;
                                }
                            }

                            stack.Push(neighborPos);
                        }
                    }

                    meshCount++;
                }
            }
        }

        if (segment.PrefabId is 604)
        {
            
        }

        return new PrefabSegmentMeshes(meshCount, voxelMeshIndex, ChunkVoxels(meshCount, voxelMeshIndex, voxels), min, max);
    }

    private static PrefabSegmentMesh[] ChunkVoxels(int meshCount, byte[] voxelMeshIndex, Voxels voxels)
    {
        PrefabSegmentMesh[] meshes = new PrefabSegmentMesh[meshCount];

        Span<byte> rawVoxels = voxels.Data;

        Span<ulong> connectsOnSide = stackalloc ulong[6];

        short3 voxelsMin = new short3(short.MaxValue, short.MaxValue, short.MaxValue);
        short3 voxelsMax = new short3(short.MinValue, short.MinValue, short.MinValue);

        for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
        {
            for (int sideIndex = 0; sideIndex < 6; sideIndex++)
            {
                int layer = Voxels.GetOuterLayerIndexForSide(sideIndex);

                ulong sideGlue = 0;

                for (int layerY = 0; layerY < Voxels.Size; layerY++)
                {
                    for (int layerX = 0; layerX < Voxels.Size; layerX++)
                    {
                        int3 voxelPos = Voxels.MapLayerPosToVoxelPos(sideIndex, layer, layerX, layerY);
                        int voxelIndex = Voxels.Index(voxelPos, 0);
                        int voxelSideIndex = Voxels.Index(voxelPos, sideIndex);

                        VoxelFace face = new VoxelFace(voxels.GetRawFace(voxelSideIndex));
                        if (face.HasGlue && !face.IsEmpty && voxelMeshIndex[voxelIndex] == meshIndex)
                        {
                            sideGlue |= 1ul << (layerX + (layerY * Voxels.Size));
                        }
                    }
                }

                connectsOnSide[sideIndex] = sideGlue;
            }

            int meshVoxelCount = 0;

            byte3 min = new byte3(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            byte3 max = new byte3(byte.MinValue, byte.MinValue, byte.MinValue);
            for (int i = 0; i < Voxels.Size * Voxels.Size * Voxels.Size; i++)
            {
                if (voxels.GetRawFace(i) != 0 && voxelMeshIndex[i] == meshIndex)
                {
                    meshVoxelCount++;

                    var pos = (byte3)int3.FromIndex(i, Voxels.Size, Voxels.Size);
                    min = byte3.Min(min, pos);
                    max = byte3.Max(max, pos);
                }
            }

            meshes[meshIndex] = new PrefabSegmentMesh(meshVoxelCount, connectsOnSide, min, max);
        }

        return meshes;
    }
}
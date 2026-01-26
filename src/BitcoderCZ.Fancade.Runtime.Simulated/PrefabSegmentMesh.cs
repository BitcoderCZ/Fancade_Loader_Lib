// <copyright file="PrefabSegmentMesh.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitcoderCZ.Buffers;
using BitcoderCZ.Maths.Vectors;
#if !NET8_0_OR_GREATER
using static BitcoderCZ.Utils.ThrowHelper;
#endif

namespace BitcoderCZ.Fancade.Runtime.Simulated;

/// <summary>
/// Stores a mesh of a prefab segment.
/// </summary>
public readonly struct PrefabSegmentMesh
{
    private readonly int _voxelCount;
    private readonly FixedArray6<ulong> _connectsOnSide;

    internal PrefabSegmentMesh(int voxelCount, ReadOnlySpan<ulong> connectsOnSide, byte3 minPos, byte3 maxPos)
    {
        _voxelCount = voxelCount;
        _connectsOnSide = FixedArray6.Create(connectsOnSide);
        MinPos = minPos;
        MaxPos = maxPos;
    }

    /// <summary>
    /// Gets the amount of voxels in the mesh.
    /// </summary>
    /// <value>Amount of voxels in the mesh.</value>
    public int VoxelCount => _voxelCount;

    /// <summary>
    /// Gets the minimum bounds of the mesh.
    /// </summary>
    /// <value>The minimum bounds of the mesh.</value>
    public byte3 MinPos { get; }

    /// <summary>
    /// Gets the maximum bounds of the mesh.
    /// </summary>
    /// <value>The maximum bounds of the mesh.</value>
    public byte3 MaxPos { get; }

    /// <summary>
    /// Gets which voxels on a given side belong to this mesh and have glue.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <listheader>
    ///     <term>Side axis</term>
    ///     <description>Indexing formula for face</description>
    /// </listheader>
    /// <item>
    ///     <term>X</term>
    ///     <description>z + y * <see cref="Voxels.Size"/></description>
    /// </item>
    /// <item>
    ///     <term>Y</term>
    ///     <description>x + z * <see cref="Voxels.Size"/>.</description>
    /// </item>
    /// <item>
    ///     <term>Z</term>
    ///     <description>x + y * <see cref="Voxels.Size"/>.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="sideIndex">Index of the side, 0 = +X, 1 = -X, 2 = +Y, 3 = -Y, 4 = +Z, 5 = -Z.</param>
    /// <returns>64 bit array, where 0 - does not have glue, 1 - has glue.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ulong GetSideGlue(int sideIndex)
        => _connectsOnSide.GetElement(sideIndex);
}
// <copyright file="ReadOnlyVoxels.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a read only view of a <see cref="Voxels"/> instance.
/// </summary>
public readonly struct ReadOnlyVoxels : IReadOnlyVoxels
{
    /// <summary>
    /// An empty <see cref="ReadOnlyVoxels"/> instance.
    /// </summary>
    public static readonly ReadOnlyVoxels Empty = default;

    internal readonly Voxels _voxels;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyVoxels"/> struct.
    /// </summary>
    /// <param name="voxels">The voxels to contruct this view over.</param>
    public ReadOnlyVoxels(Voxels voxels)
    {
        _voxels = voxels;
    }

    /// <summary>
    /// Gets the raw voxel data.
    /// </summary>
    /// <value>The raw voxel data; or an empty span, if <see cref="IsEmpty"/> is <see langword="true"/>.</value>
    public ReadOnlySpan<byte> Data => _voxels.Data;

    /// <summary>
    /// Gets a value indicating whether this <see cref="ReadOnlyVoxels"/> instance is empty.
    /// </summary>
    /// <remarks>
    /// <see cref="GetRawFace(int)"/> will throw when <see cref="IsEmpty"/> is <see langword="true"/>.
    /// </remarks>
    /// <value><see langword="true"/> if the <see cref="ReadOnlyVoxels"/> instance is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => _voxels.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether all voxels in the <see cref="ReadOnlyVoxels"/> instance are empty.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="IsEmpty"/> is <see langword="true"/> or all of the voxels are empty; otherwise, <see langword="false"/>.</value>
    public bool AllVoxelsEmpty => _voxels.AllVoxelsEmpty;

    /// <summary>
    /// Gets the voxel at the specified position.
    /// </summary>
    /// <param name="position">Position of the voxel to get/set.</param>
    /// <returns>The <see cref="Voxel"/> at <paramref name="position"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> is out of bounds.</exception>
    public Voxel this[int3 position] => _voxels[position];

    /// <summary>
    /// Converts a <see cref="ReadOnlyVoxels"/> view over a <see cref="Voxels"/> instance.
    /// </summary>
    /// <param name="voxels">The <see cref="Voxels"/> instance.</param>
    public static implicit operator ReadOnlyVoxels(Voxels voxels)
        => new(voxels);

    /// <summary>
    /// Writes the face glue data into a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> means that the face has glue; <see langword="false"/> means that the face does not have glue.
    /// </remarks>
    /// <param name="destination">The <see cref="BitArray"/> to write into, must be at least <see cref="Voxels.VoxelCount"/> * 6 elements long.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> has less than <see cref="Voxels.VoxelCount"/> * 6 elements.</exception>
    public void WriteFaceGlueInfo(BitArray destination)
        => _voxels.WriteFaceGlueInfo(destination);

    /// <summary>
    /// Gets the face glue data as a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> means that the face has glue; <see langword="false"/> means that the face does not have glue.
    /// </remarks>
    /// <returns>The <see cref="BitArray"/> with the face data..</returns>
    public BitArray GetFaceGlueInfo()
        => _voxels.GetFaceGlueInfo();

    #region Read

    /// <summary>
    /// Gets the face at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    public VoxelFace GetFace(int3 position, int faceIndex)
        => _voxels.GetFace(position, faceIndex);

    /// <summary>
    /// Gets the face at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    public VoxelFace GetFaceUnchecked(int3 position, int faceIndex)
        => _voxels.GetFaceUnchecked(position, faceIndex);

    /// <summary>
    /// Gets the raw face data at the specified index.
    /// </summary>
    /// <param name="dataIndex">Index of the face to retrieve, can be calculated using <see cref="Voxels.Index(int3, int)"/>.</param>
    /// <returns>The raw face data at the specified index.</returns>
    /// <exception cref="NullReferenceException">Thrown when the <see cref="ReadOnlyVoxels"/> instance is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetRawFace(int dataIndex)
        => _voxels.GetRawFace(dataIndex);
    #endregion
}

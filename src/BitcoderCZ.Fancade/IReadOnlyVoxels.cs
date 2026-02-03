// <copyright file="IReadOnlyVoxels.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BitcoderCZ.Maths.Vectors;
using BitcoderCZ.Utils;

namespace BitcoderCZ.Fancade;

/// <summary>
/// A base interface for <see cref="Voxels"/> and <see cref="ReadOnlyVoxels"/>.
/// </summary>
public interface IReadOnlyVoxels
{
    /// <summary>
    /// Gets the raw voxel data.
    /// </summary>
    /// <value>The raw voxel data, with a length of <see cref="Voxels.VoxelCount"/> * 6; or an empty span, if <see cref="IsEmpty"/> is <see langword="true"/>.</value>
    [UnscopedRef]
    ReadOnlySpan<byte> Data { get; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="IReadOnlyVoxels"/> instance is empty.
    /// </summary>
    /// <remarks>
    /// <see cref="GetRawFace(int)"/> will throw when <see cref="IsEmpty"/> is <see langword="true"/>.
    /// </remarks>
    /// <value><see langword="true"/> if the <see cref="IReadOnlyVoxels"/> instance is empty; otherwise, <see langword="false"/>.</value>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets a value indicating whether all voxels in the <see cref="IReadOnlyVoxels"/> instance are empty.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="IsEmpty"/> is <see langword="true"/> or all of the voxels are empty; otherwise, <see langword="false"/>.</value>
    bool AllVoxelsEmpty { get; }

    /// <summary>
    /// Gets the voxel at the specified position.
    /// </summary>
    /// <param name="position">Position of the voxel to get/set.</param>
    /// <returns>The <see cref="Voxel"/> at <paramref name="position"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> is out of bounds.</exception>
    Voxel this[int3 position] { get; }

    /// <summary>
    /// Writes the face glue data into a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> means that the face has glue; <see langword="false"/> means that the face does not have glue.
    /// </remarks>
    /// <param name="destination">The <see cref="BitArray"/> to write into, must be at least <see cref="Voxels.VoxelCount"/> * 6 elements long.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> has less than <see cref="Voxels.VoxelCount"/> * 6 elements.</exception>
    virtual void WriteFaceGlueInfo(BitArray destination)
    {
        ThrowHelper.ThrowIfLessThan(destination.Length, Voxels.VoxelCount * 6);

        if (IsEmpty)
        {
            for (int i = 0; i < Voxels.VoxelCount * 6; i++)
            {
                destination[i] = false;
            }
        }
        else
        {
            var data = Data;

            for (int i = 0; i < Voxels.VoxelCount * 6; i++)
            {
                destination[i] = (data[i] & 0b_1000_0000) != 0;
            }
        }
    }

    /// <summary>
    /// Gets the face glue data as a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> means that the face has glue; <see langword="false"/> means that the face does not have glue.
    /// </remarks>
    /// <returns>The <see cref="BitArray"/> with the face data..</returns>
    virtual BitArray GetFaceGlueInfo()
    {
        var array = new BitArray(Voxels.VoxelCount * 6);
        WriteFaceGlueInfo(array);
        return array;
    }

    /// <summary>
    /// Gets the face at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    VoxelFace GetFace(int3 position, int faceIndex);

    /// <summary>
    /// Gets the face at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    VoxelFace GetFaceUnchecked(int3 position, int faceIndex);

    /// <summary>
    /// Gets the raw face data at the specified index.
    /// </summary>
    /// <param name="dataIndex">Index of the face to retrieve, can be calculated using <see cref="Voxels.Index(int3, int)"/>.</param>
    /// <returns>The raw face data at the specified index.</returns>
    /// <exception cref="NullReferenceException">Thrown when the <see cref="IReadOnlyVoxels"/> instance is empty.</exception>
    byte GetRawFace(int dataIndex);
}

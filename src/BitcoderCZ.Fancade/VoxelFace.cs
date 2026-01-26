// <copyright file="VoxelFace.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents a face of a voxel.
/// </summary>
public readonly struct VoxelFace
{
    private readonly byte _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoxelFace"/> struct.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    public VoxelFace(byte data)
    {
        _data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoxelFace"/> struct.
    /// </summary>
    /// <param name="color">Color of the <see cref="VoxelFace"/>.</param>
    /// <param name="hasGlue"><see langword="true"/> if the <see cref="VoxelFace"/> has "glue"; otherwise, <see langword="false"/>.</param>
    public VoxelFace(FcColor color, bool hasGlue)
    {
        _data = CreateFace(color, hasGlue);
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="VoxelFace"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="VoxelFace"/> is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => GetIsEmpty(_data);

    /// <summary>
    /// Gets the color of the <see cref="VoxelFace"/>.
    /// </summary>
    /// <value>Color of the <see cref="VoxelFace"/>.</value>
    public FcColor Color
    {
        get => ExtractColor(_data);
        init => SetColor(_data, value);
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="VoxelFace"/> has "glue".
    /// </summary>
    /// <value><see langword="true"/> if the <see cref="VoxelFace"/> has "glue"; otherwise, <see langword="false"/>.</value>
    public bool HasGlue
    {
        get => ExtractGlue(_data);
        init => SetGlue(_data, value);
    }

    /// <summary>
    /// Converts a <see cref="VoxelFace"/> to it's raw data.
    /// </summary>
    /// <param name="value">The <see cref="VoxelFace"/> to convert.</param>
    public static implicit operator byte(VoxelFace value)
        => value._data;

    /// <summary>
    /// Converts the raw face data to <see cref="VoxelFace"/>.
    /// </summary>
    /// <param name="value">The data to convert.</param>
    public static explicit operator VoxelFace(byte value)
        => new VoxelFace(value);

    /// <summary>
    /// Creates a face given color and glue.
    /// </summary>
    /// <param name="color">Color of the <see cref="VoxelFace"/>.</param>
    /// <param name="hasGlue"><see langword="true"/> if the <see cref="VoxelFace"/> has "glue"; otherwise, <see langword="false"/>.</param>
    /// <returns>The created face.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte CreateFace(FcColor color, bool hasGlue)
        => (byte)((int)color | (hasGlue ? 0 : 0b_1000_0000));

    /// <summary>
    /// Gets whether a face has glue.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    /// <returns><see langword="true"/> if the face has glue; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractGlue(byte data)
        => (data & 0b_1000_0000) == 0;

    /// <summary>
    /// Gets the face's color.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    /// <returns>The face's color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FcColor ExtractColor(byte data)
        => (FcColor)(data & 0b_0111_1111);

    /// <summary>
    ///  Gets a value indicating whether the face is empty.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    /// <returns><see langword="true"/> if the face is empty; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetIsEmpty(byte data)
        => data == 0;

    /// <summary>
    /// Sets the glue of a face.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    /// <param name="hasGlue"><see langword="true"/> if the face should have "glue"; otherwise, <see langword="false"/>.</param>
    /// <returns>The face with the glue.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetGlue(byte data, bool hasGlue)
        => (byte)((data & 0b_0111_1111) | (hasGlue ? 0 : 0b_1000_0000));

    /// <summary>
    /// Sets the color of a face.
    /// </summary>
    /// <param name="data">The raw face data.</param>
    /// <param name="color">The new color.</param>
    /// <returns>The face with the new color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SetColor(byte data, FcColor color)
        => (byte)((data & 0b_1000_0000) | (int)color);
}

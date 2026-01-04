// <copyright file="Voxels.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents the mesh of a <see cref="PrefabSegment"/>.
/// </summary>
public readonly struct Voxels : ICloneable
{
    /// <summary>
    /// Amount of voxels in one dimension.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// The total amount of voxels in the mesh.
    /// </summary>
    public const int VoxelCount = Size * Size * Size;

    /// <summary>
    /// An empty <see cref="Voxels"/> instance.
    /// </summary>
    public static readonly Voxels Empty = default;

    internal readonly byte[]? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Voxels"/> struct, that is not empty.
    /// </summary>
    public Voxels()
    {
        _data = new byte[VoxelCount * 6];
    }

    internal Voxels(byte[] data)
    {
        Debug.Assert(data.Length == VoxelCount * 6, $"{nameof(data)} must be of length {VoxelCount * 6}.");

        _data = data;
    }

    /// <summary>
    /// Gets the raw voxel data.
    /// </summary>
    /// <value>The raw voxel data; or an empty span, if <see cref="IsEmpty"/> is <see langword="true"/>.</value>
    public Span<byte> Data => _data is null
        ? []
        : _data.AsSpan();

    /// <summary>
    /// Gets a value indicating whether this <see cref="Voxels"/> instance is empty.
    /// </summary>
    /// <remarks>
    /// All write methods and <see cref="GetRawFace(int)"/> will throw when <see cref="IsEmpty"/> is <see langword="true"/>.
    /// </remarks>
    /// <value><see langword="true"/> if the <see cref="Voxels"/> instance is empty; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => _data is null;

    /// <summary>
    /// Gets a value indicating whether all voxels in the <see cref="Voxels"/> instance are empty.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="IsEmpty"/> is <see langword="true"/> or all of the voxels are empty; otherwise, <see langword="false"/>.</value>
    public bool AllVoxelsEmpty
    {
        get
        {
            if (IsEmpty)
            {
                return true;
            }

            Debug.Assert(_data is not null, $"{nameof(_data)} should not be null.");

#if NET7_0_OR_GREATER
            return _data.AsSpan().IndexOfAnyExcept((byte)0) < 0;
#else
            for (int i = 0; i < _data.Length; i++)
            {
                if (_data[i] != 0)
                {
                    return false;
                }
            }

            return true;
#endif
        }
    }

    /// <summary>
    /// Gets or sets the voxel at the specified position.
    /// </summary>
    /// <param name="position">Position of the voxel to get/set.</param>
    /// <returns>The <see cref="Voxel"/> at <paramref name="position"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown when writing and the <see cref="Voxels"/> instance is empty.</exception>
    public Voxel this[int3 position]
    {
        get
        {
            CheckPosition(position);

            if (_data is null)
            {
                return default;
            }

            int index = Index(position, 0);

            Voxel voxel = default;
            voxel[0] = (VoxelFace)_data[index + (0 * VoxelCount)];
            voxel[1] = (VoxelFace)_data[index + (1 * VoxelCount)];
            voxel[2] = (VoxelFace)_data[index + (2 * VoxelCount)];
            voxel[3] = (VoxelFace)_data[index + (3 * VoxelCount)];
            voxel[4] = (VoxelFace)_data[index + (4 * VoxelCount)];
            voxel[5] = (VoxelFace)_data[index + (5 * VoxelCount)];

            return voxel;
        }

        set
        {
            CheckPosition(position);
            CheckNotEmpty();

            int index = Index(position, 0);

            _data[index + (0 * VoxelCount)] = value[0];
            _data[index + (1 * VoxelCount)] = value[1];
            _data[index + (2 * VoxelCount)] = value[2];
            _data[index + (3 * VoxelCount)] = value[3];
            _data[index + (4 * VoxelCount)] = value[4];
            _data[index + (5 * VoxelCount)] = value[5];
        }
    }

    /// <summary>
    /// Converts a position and a face to an index into the underlying data array.
    /// </summary>
    /// <param name="position">The position to convert.</param>
    /// <param name="faceIndex">The face to convert; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The corresponding index for <paramref name="position"/> and <paramref name="faceIndex"/>.</returns>
    public static int Index(int3 position, int faceIndex)
        => position.X + (position.Y * Size) + (position.Z * Size * Size) + (faceIndex * VoxelCount);

    /// <summary>
    /// Gets whether <paramref name="position"/> is within the bounds of the voxel mesh.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns><see langword="true"/> if <paramref name="position"/> is in bounds; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds(int3 position)
        => position.InBounds(Size, Size, Size);

    /// <summary>
    /// Gets the index of the layer that is facing the outside for a given side.
    /// </summary>
    /// <param name="sideIndex">Index of the side, 0-5, +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>Index of the layer that is facing the outside.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOuterLayerIndexForSide(int sideIndex)
        => sideIndex is 0 or 2 or 4 ? Size - 1 : 0;

    /// <summary>
    /// Converts a layer position to a voxel position.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <listheader>
    ///     <term>Side axis</term>
    ///     <description>Position formula for side.</description>
    /// </listheader>
    /// <item>
    ///     <term>X</term>
    ///     <description>X: <paramref name="layer"/>, Y: <paramref name="layerY"/>, Z: <paramref name="layerX"/>.</description>
    /// </item>
    /// <item>
    ///     <term>Y</term>
    ///     <description>X: <paramref name="layerX"/>, Y: <paramref name="layer"/>, Z: <paramref name="layerY"/>.</description>
    /// </item>
    /// <item>
    ///     <term>Z</term>
    ///     <description>X: <paramref name="layerX"/>, Y: <paramref name="layerY"/>, Z: <paramref name="layer"/>.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="sideIndex">Index of the side, 0 = +X, 1 = -X, 2 = +Y, 3 = -Y, 4 = +Z, 5 = -Z.</param>
    /// <param name="layer">
    /// The position of the layer along the axis perpendicular to the side. 
    /// 0 corresponds to the negative-most layer, and <see cref="Size"/> - 1 corresponds to the positive-most layer. 
    /// This convention is the same for both positive and negative sides of an axis (e.g., +X and -X).
    /// </param>
    /// <param name="layerX">The layer x position.</param>
    /// <param name="layerY">The layer y position.</param>
    /// <returns>The voxel position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 MapLayerPosToVoxelPos(int sideIndex, int layer, int layerX, int layerY)
        => sideIndex switch
        {
            0 or 1 => new int3(layer, layerY, layerX),
            2 or 3 => new int3(layerX, layer, layerY),
            4 or 5 => new int3(layerX, layerY, layer),
            _ => default,
        };

    /// <summary>
    /// Writes the face glue data into a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> means that the face has glue; <see langword="true"/> means that the face does not have glue.
    /// </remarks>
    /// <param name="destination">The <see cref="BitArray"/> to write into, must be at least <see cref="VoxelCount"/> * 6 elements long.</param>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> has less than <see cref="VoxelCount"/> * 6 elements.</exception>
    public void WriteFaceGlueInfo(BitArray destination)
    {
        CheckNotEmpty();
        ThrowIfLessThan(destination.Length, VoxelCount * 6);

        for (int i = 0; i < VoxelCount * 6; i++)
        {
            destination[i] = (_data[i] & 0b_1000_0000) != 0;
        }
    }

    /// <summary>
    /// Gets the face glue data as a <see cref="BitArray"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> means that the face has glue; <see langword="true"/> means that the face does not have glue.
    /// </remarks>
    /// <returns>The <see cref="BitArray"/> with the face data..</returns>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public BitArray GetFaceGlueInfo()
    {
        CheckNotEmpty();

        var array = new BitArray(VoxelCount * 6);
        WriteFaceGlueInfo(array);
        return array;
    }

    /// <summary>
    /// Gets a reference to the voxel at the specified position.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <returns>A reference to the specified voxel.</returns>
    public Ref GetVoxelRef(int3 position)
    {
        CheckPosition(position);

#if NET7_0_OR_GREATER
        return new Ref(ref _data![Index(position, 0)]);
#else
        return new Ref(this, Index(position, 0));
#endif
    }

    #region Read

    /// <summary>
    /// Gets the face at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    public VoxelFace GetFace(int3 position, int faceIndex)
    {
        CheckPositionAndFace(position, faceIndex);

        return _data is null
            ? default
            : (VoxelFace)_data[Index(position, faceIndex)];
    }

    /// <summary>
    /// Gets the face at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <returns>The face at the specified position.</returns>
    public VoxelFace GetFaceUnchecked(int3 position, int faceIndex)
        => _data is null
            ? default
            : (VoxelFace)_data[Index(position, faceIndex)];

    /// <summary>
    /// Gets the raw face data at the specified index.
    /// </summary>
    /// <param name="dataIndex">Index of the face to retrieve, can be calculated using <see cref="Index(int3, int)"/>.</param>
    /// <returns>The raw face data at the specified index.</returns>
    /// <exception cref="NullReferenceException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetRawFace(int dataIndex)
        => _data![dataIndex];
    #endregion
    #region Write

    /// <summary>
    /// Makes all voxels empty.
    /// </summary>
    public void Clear()
        => Data.Clear();

    /// <summary>
    /// Fills the <see cref="Voxels"/> with a <see cref="Voxel"/>.
    /// </summary>
    /// <param name="voxel">The <see cref="Voxel"/> to fill the <see cref="Voxels"/> with.</param>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void Fill(Voxel voxel)
    {
        CheckNotEmpty();

        var data = _data.AsSpan();

        for (int p = 0; p < 6; p++)
        {
            data.Slice(p * VoxelCount, VoxelCount).Fill((byte)voxel[p]);
        }
    }

    /// <summary>
    /// Sets the face at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to set.</param>
    /// <param name="faceIndex">The face to set; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="face">The new <see cref="VoxelFace"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetFace(int3 position, int faceIndex, VoxelFace face)
    {
        CheckPositionAndFace(position, faceIndex);
        CheckNotEmpty();

        _data[Index(position, faceIndex)] = face;
    }

    /// <summary>
    /// Sets the face at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to get.</param>
    /// <param name="faceIndex">The face to get; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="face">The new <see cref="VoxelFace"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetFaceUnchecked(int3 position, int faceIndex, VoxelFace face)
    {
        CheckNotEmpty();

        _data[Index(position, faceIndex)] = face;
    }

    /// <summary>
    /// Sets the color at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to set.</param>
    /// <param name="faceIndex">The face to set; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="color">The new color.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetColor(int3 position, int faceIndex, FcColor color)
    {
        CheckPositionAndFace(position, faceIndex);
        CheckNotEmpty();

        int index = Index(position, faceIndex);
        _data[index] = (byte)((int)color | _data[index] & 0b_1000_0000);
    }

    /// <summary>
    /// Sets the color at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to set.</param>
    /// <param name="faceIndex">The face to set; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="color">The new color.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="position"/> or <paramref name="faceIndex"/> is out of bounds.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetColorUnchecked(int3 position, int faceIndex, FcColor color)
    {
        CheckPositionAndFace(position, faceIndex);
        CheckNotEmpty();

        int index = Index(position, faceIndex);
        _data[index] = (byte)((int)color | _data[index] & 0b_1000_0000);
    }

    /// <summary>
    /// Sets the glue at the specified position and face index.
    /// </summary>
    /// <param name="position">Position of the voxel to set.</param>
    /// <param name="faceIndex">The face to set; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="glue">The new glue value.</param>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetGlue(int3 position, int faceIndex, bool glue)
    {
        CheckNotEmpty();

        int index = Index(position, faceIndex);
        _data[index] = (byte)((glue ? 0 : 0b_1000_0000) | _data[index] & 0b_0111_1111);
    }

    /// <summary>
    /// Sets the glue at the specified position and face index without bounds checking.
    /// </summary>
    /// <param name="position">Position of the voxel to set.</param>
    /// <param name="faceIndex">The face to set; order is: +X, -X, +Y, -Y, +Z, -Z.</param>
    /// <param name="glue">The new glue value.</param>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    public void SetGlueUnchecked(int3 position, int faceIndex, bool glue)
    {
        CheckNotEmpty();

        int index = Index(position, faceIndex);
        _data[index] = (byte)((glue ? 0 : 0b_1000_0000) | _data[index] & 0b_0111_1111);
    }

    /// <summary>
    /// Sets the raw face data at the specified index.
    /// </summary>
    /// <param name="dataIndex">Index of the face to set, can be calculated using <see cref="Index(int3, int)"/>.</param>
    /// <param name="face">The new face.</param>
    /// <exception cref="NullReferenceException">Thrown when the <see cref="Voxels"/> instance is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRawFace(int dataIndex, byte face)
        => _data![dataIndex] = face;
    #endregion

    /// <summary>
    /// Creates a copy of the <see cref="Voxels"/>.
    /// </summary>
    /// <returns>A copy of the <see cref="Voxels"/>.</returns>
    public Voxels Clone()
        => _data is null ? Empty : new Voxels((byte[])_data.Clone());

    /// <inheritdoc/>
    object ICloneable.Clone()
        => Clone();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckPosition(int3 position)
    {
        if (!InBounds(position))
        {
            ThrowArgumentOutOfRangeException(nameof(position), $"{nameof(position)} is out of bounds.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckFace(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex > 5)
        {
            ThrowArgumentOutOfRangeException(nameof(faceIndex), $"{nameof(faceIndex)} must be between 0 and 5.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckPositionAndFace(int3 position, int faceIndex)
    {
        CheckPosition(position);
        CheckFace(faceIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MemberNotNull(nameof(_data))]
    private void CheckNotEmpty()
    {
        if (_data is null)
        {
            ThrowInvalidOperationException("The Voxels instance is empty. Cannot perform this operation.");
        }
    }

    /// <summary>
    /// Represents a reference to a voxel in a <see cref="Voxels"/> instance.
    /// </summary>
    public readonly ref struct Ref
    {
#if NET7_0_OR_GREATER
        private readonly ref byte _ref;
#else
        private readonly Voxels _voxels;
        private readonly int _index;
#endif

#if NET7_0_OR_GREATER
        internal Ref(ref byte @ref)
        {
            _ref = ref @ref;
        }
#else
        internal Ref(Voxels voxels, int index)
        {
            Debug.Assert(voxels._data is not null, $"{nameof(voxels)}.{nameof(voxels._data)} should not be null.");

            _voxels = voxels;
            _index = index;
        }
#endif

        /// <summary>
        /// Gets or sets the face of the voxel at the specified index (sets both color and glue).
        /// </summary>
        /// <param name="faceIndex">Index of the face to get/set.</param>
        /// <returns>The face at <paramref name="faceIndex"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="faceIndex"/> is out of bounds.</exception>
        public VoxelFace this[int faceIndex]
        {
            readonly get
            {
                CheckFace(faceIndex);

#if NET7_0_OR_GREATER
                return new VoxelFace(Unsafe.Add(ref _ref, faceIndex * VoxelCount));
#else
                return new VoxelFace(_voxels._data![_index + (faceIndex * VoxelCount)]);
#endif
            }

            set
            {
                CheckFace(faceIndex);

#if NET7_0_OR_GREATER
                Unsafe.Add(ref _ref, faceIndex * VoxelCount) = (byte)value;
#else
                _voxels._data![_index + (faceIndex * VoxelCount)] = (byte)value;
#endif
            }
        }

        /// <summary>
        /// Sets the color of the specified face.
        /// </summary>
        /// <param name="faceIndex">Index to the face to set.</param>
        /// <param name="color">The new color of the face.</param>
        public void SetColor(int faceIndex, FcColor color)
        {
            CheckFace(faceIndex);

#if NET7_0_OR_GREATER
            ref byte faceRef = ref Unsafe.Add(ref _ref, faceIndex * VoxelCount);
            faceRef = (byte)((int)color | faceRef & 0b_1000_0000);
#else
            int index = _index + (faceIndex * VoxelCount);
            _voxels._data![index] = (byte)((int)color | _voxels._data![index] & 0b_1000_0000);
#endif
        }

        /// <summary>
        /// Sets the color of the specified face.
        /// </summary>
        /// <param name="faceIndex">Index to the face to set.</param>
        /// <param name="glue">The new glue value.</param>
        public void SetGlue(int faceIndex, bool glue)
        {
            CheckFace(faceIndex);

#if NET7_0_OR_GREATER
            ref byte faceRef = ref Unsafe.Add(ref _ref, faceIndex * VoxelCount);
            faceRef = (byte)((glue ? 0 : 0b_1000_0000) | faceRef & 0b_0111_1111);
#else
            int index = _index + (faceIndex * VoxelCount);
            _voxels._data![index] = (byte)((glue ? 0 : 0b_1000_0000) | _voxels._data![index] & 0b_0111_1111);
#endif
        }
    }
}

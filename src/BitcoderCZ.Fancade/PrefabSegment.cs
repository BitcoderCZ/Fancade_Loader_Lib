// <copyright file="PrefabSegment.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using BitcoderCZ.Maths.Vectors;
using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade;

/// <summary>
/// Represents the mesh part of a <see cref="Prefab"/>.
/// </summary>
public class PrefabSegment : ICloneable
{
    /// <summary>
    /// The number of voxels in a segment.
    /// </summary>
    [Obsolete($"Use {nameof(Fancade.Voxels)}.{nameof(Fancade.Voxels.VoxelCount)}")]
    public const int NumbVoxels = 8 * 8 * 8;

    /// <summary>
    /// A mask to get the color from a voxel side.
    /// </summary>
    [Obsolete($"Use {nameof(VoxelFace)} to extract color and glue info.")]
    public const byte ColorMask = 0b_0111_1111;

    /// <summary>
    /// A mask to get the attribs from a voxel side.
    /// </summary>
    [Obsolete($"Use {nameof(VoxelFace)} to extract color and glue info.")]
    public const byte AttribsMask = 0b_1000_0000;

    // ideally would be a property, but .Voxels[...] = ... wouldn't work

    /// <summary>
    /// Gets or sets the voxels/model of the <see cref="PrefabSegment"/>.
    /// </summary>
    /// <value>Voxels/model of the <see cref="PrefabSegment"/>.</value>
    public Voxels Voxels;

    private int3 _posInPrefab;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSegment"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this segment is in.</param>
    /// <param name="posInPrefab">Position of this segment in prefab.</param>
    public PrefabSegment(ushort prefabId, int3 posInPrefab)
    {
        PrefabId = prefabId;
        PosInPrefab = posInPrefab;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSegment"/> class.
    /// </summary>
    /// <param name="prefabId">Id of the prefab this segment is in.</param>
    /// <param name="posInPrefab">Position of this segment in prefab.</param>
    /// <param name="voxels">Voxels/model of this prefab.</param>
    public PrefabSegment(ushort prefabId, int3 posInPrefab, Voxels voxels)
    {
        PrefabId = prefabId;
        PosInPrefab = posInPrefab;
        Voxels = voxels;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrefabSegment"/> class.
    /// </summary>
    /// <param name="other">The segment to copy.</param>
    public PrefabSegment(PrefabSegment other)
        : this(other.PrefabId, other.PosInPrefab, other.Voxels.IsEmpty ? Voxels.Empty : other.Voxels.Clone())
    {
    }

    /// <summary>
    /// Gets the id of the prefab this segment is in.
    /// </summary>
    /// <value>Id of the prefab this segment is in.</value>
    public ushort PrefabId { get; internal set; }

    /// <summary>
    /// Gets the position of this segment in prefab.
    /// </summary>
    /// <value>Position of this segment in prefab.</value>
    public int3 PosInPrefab
    {
        get => _posInPrefab;
        internal set
        {
            if (value.X >= Prefab.MaxSize || value.Y >= Prefab.MaxSize || value.Z >= Prefab.MaxSize)
            {
                ThrowArgumentOutOfRangeException(nameof(value), $"{nameof(PosInPrefab)} cannot be larger than or equal to {Prefab.MaxSize}.");
            }
            else if (value.X < 0 || value.Y < 0 || value.Z < 0)
            {
                ThrowArgumentOutOfRangeException(nameof(value), $"{nameof(PosInPrefab)} cannot be negative.");
            }

            _posInPrefab = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the <see cref="PrefabSegment"/> is empty.
    /// </summary>
    /// <value><see cref="Fancade.Voxels.AllVoxelsEmpty"/>.</value>
    public bool IsEmpty => Voxels.AllVoxelsEmpty;

    /// <summary>
    /// Converts a 3D index into a 1D index, used to índex into <see cref="Voxels"/>.
    /// </summary>
    /// <remarks>
    /// Does not check that <paramref name="pos"/> is in bounds.
    /// </remarks>
    /// <param name="pos">The 3D index.</param>
    /// <returns>The converted 1D index.</returns>
    [Obsolete]
    public static int IndexVoxels(int3 pos)
        => pos.ToIndex(8, 8);

    /// <summary>
    /// Converts raw voxel data to <see cref="Voxels"/>.
    /// </summary>
    /// <param name="voxels">The voxel data to convert.</param>
    /// <returns>The converted <see cref="Voxels"/>.</returns>
    public static unsafe Voxels VoxelsFromRaw(ReadOnlySpan<byte> voxels)
    {
        if (voxels.Length < Voxels.VoxelCount * 6)
        {
            ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {Voxels.VoxelCount * 6}.", nameof(voxels));
        }

        return new Voxels(voxels.ToArray());
    }

    /// <summary>
    /// Converts <see cref="Voxels"/> to raw voxel data.
    /// </summary>
    /// <param name="voxels">The <see cref="Voxels"/> to convert.</param>
    /// <returns>The converted raw voxel data.</returns>
    public static unsafe byte[] VoxelsToRaw(Voxels voxels)
    {
        ThrowIfNull(voxels, nameof(voxels));

        return voxels._data is null ? new byte[Voxels.VoxelCount * 6] : (byte[])voxels._data.Clone();
    }

    /// <summary>
    /// Converts <see cref="Voxels"/> to raw voxel data.
    /// </summary>
    /// <param name="voxels">The <see cref="Voxels"/> to convert.</param>
    /// <param name="destination">The destination span.</param>
    public static unsafe void VoxelsToRaw(Voxels voxels, Span<byte> destination)
    {
        if (destination.Length < Voxels.VoxelCount * 6)
        {
            ThrowArgumentException($"{nameof(destination)}'s length must be greater than or equal to {Voxels.VoxelCount * 6}.", nameof(destination));
        }

        voxels._data.CopyTo(destination);
    }

    /// <summary>
    /// Creates a deep copy of this <see cref="PrefabSegment"/>.
    /// </summary>
    /// <returns>A deep copy of this <see cref="PrefabSegment"/>.</returns>
    public PrefabSegment Clone()
        => new PrefabSegment(this);

    /// <inheritdoc/>
    object ICloneable.Clone()
        => new PrefabSegment(this);
}
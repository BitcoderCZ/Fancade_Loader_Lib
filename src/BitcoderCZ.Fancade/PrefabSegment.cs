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
    public const int NumbVoxels = 8 * 8 * 8;

    /// <summary>
    /// A mask to get the color from a voxel side.
    /// </summary>
    public const byte ColorMask = 0b_0111_1111;

    /// <summary>
    /// A mask to get the attribs from a voxel side.
    /// </summary>
    public const byte AttribsMask = 0b_1000_0000;

    private Voxel[]? _voxels;

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
    public PrefabSegment(ushort prefabId, int3 posInPrefab, Voxel[]? voxels)
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
        : this(other.PrefabId, other.PosInPrefab, other.Voxels is null ? null : (Voxel[])other.Voxels.Clone())
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
    /// Gets or sets the voxels/model of this segment.
    /// </summary>
    /// <remarks>
    /// <para>Must be 8*8*8 (512) long.</para>
    /// <para>The voxels are in XYZ order.</para>
    /// </remarks>
    /// <value>Voxels/model of this segment.</value>
    public Voxel[]? Voxels
    {
        get => _voxels;
        set
        {
            if (value is not null && value.Length != NumbVoxels)
            {
                ThrowArgumentException($"{nameof(Voxels)} must be {NumbVoxels} long, but {nameof(value)}.Length is {value.Length}.", nameof(value));
            }

            _voxels = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="PrefabSegment"/> is empty.
    /// </summary>
    /// <value><see langword="true"/> if <see cref="Voxels"/> is null or <see cref="Voxel.IsEmpty"/> is true for all of the voxels; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty
    {
        get
        {
            if (Voxels is null)
            {
                return true;
            }

            for (int i = 0; i < Voxels.Length; i++)
            {
                if (!Voxels[i].IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Converts a 3D index into a 1D index, used to índex into <see cref="Voxels"/>.
    /// </summary>
    /// <remarks>
    /// Does not check that <paramref name="pos"/> is in bounds.
    /// </remarks>
    /// <param name="pos">The 3D index.</param>
    /// <returns>The converted 1D index.</returns>
    public static int IndexVoxels(int3 pos)
        => pos.ToIndex(8, 8);

    /// <summary>
    /// Converts raw voxel data to <see cref="Voxel"/>s.
    /// </summary>
    /// <param name="voxels">The voxel data to convert.</param>
    /// <returns>The converted <see cref="Voxel"/>s.</returns>
    public static unsafe Voxel[] VoxelsFromRaw(byte[] voxels)
    {
        ThrowIfNull(voxels, nameof(voxels));

        if (voxels.Length < NumbVoxels * 6)
        {
            ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(voxels));
        }

        Voxel[] result = new Voxel[NumbVoxels];

        for (int i = 0; i < NumbVoxels; i++)
        {
            Voxel voxel = default;
            byte s0 = voxels[i + (NumbVoxels * 0)];
            byte s1 = voxels[i + (NumbVoxels * 1)];
            byte s2 = voxels[i + (NumbVoxels * 2)];
            byte s3 = voxels[i + (NumbVoxels * 3)];
            byte s4 = voxels[i + (NumbVoxels * 4)];
            byte s5 = voxels[i + (NumbVoxels * 5)];

            voxel.Colors[0] = (byte)(s0 & ColorMask);
            voxel.Colors[1] = (byte)(s1 & ColorMask);
            voxel.Colors[2] = (byte)(s2 & ColorMask);
            voxel.Colors[3] = (byte)(s3 & ColorMask);
            voxel.Colors[4] = (byte)(s4 & ColorMask);
            voxel.Colors[5] = (byte)(s5 & ColorMask);
            voxel.Attribs[0] = UnsafeUtils.BitCast<byte, bool>((byte)((s0 & AttribsMask) >> 7));
            voxel.Attribs[1] = UnsafeUtils.BitCast<byte, bool>((byte)((s1 & AttribsMask) >> 7));
            voxel.Attribs[2] = UnsafeUtils.BitCast<byte, bool>((byte)((s2 & AttribsMask) >> 7));
            voxel.Attribs[3] = UnsafeUtils.BitCast<byte, bool>((byte)((s3 & AttribsMask) >> 7));
            voxel.Attribs[4] = UnsafeUtils.BitCast<byte, bool>((byte)((s4 & AttribsMask) >> 7));
            voxel.Attribs[5] = UnsafeUtils.BitCast<byte, bool>((byte)((s5 & AttribsMask) >> 7));

            result[i] = voxel;
        }

        return result;
    }

    /// <summary>
    /// Converts raw voxel data to <see cref="Voxel"/>s.
    /// </summary>
    /// <param name="voxels">The voxel data to convert.</param>
    /// <param name="destination">The destination span.</param>
    public static unsafe void VoxelsFromRaw(ReadOnlySpan<byte> voxels, Span<Voxel> destination)
    {
        if (voxels.Length < NumbVoxels * 6)
        {
            ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(voxels));
        }

        if (destination.Length < NumbVoxels)
        {
            ThrowArgumentException($"{nameof(destination)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
        }

        for (int i = 0; i < NumbVoxels; i++)
        {
            Voxel voxel = default;
            byte s0 = voxels[i + (NumbVoxels * 0)];
            byte s1 = voxels[i + (NumbVoxels * 1)];
            byte s2 = voxels[i + (NumbVoxels * 2)];
            byte s3 = voxels[i + (NumbVoxels * 3)];
            byte s4 = voxels[i + (NumbVoxels * 4)];
            byte s5 = voxels[i + (NumbVoxels * 5)];

            voxel.Colors[0] = (byte)(s0 & ColorMask);
            voxel.Colors[1] = (byte)(s1 & ColorMask);
            voxel.Colors[2] = (byte)(s2 & ColorMask);
            voxel.Colors[3] = (byte)(s3 & ColorMask);
            voxel.Colors[4] = (byte)(s4 & ColorMask);
            voxel.Colors[5] = (byte)(s5 & ColorMask);
            voxel.Attribs[0] = UnsafeUtils.BitCast<byte, bool>((byte)((s0 & AttribsMask) >> 7));
            voxel.Attribs[1] = UnsafeUtils.BitCast<byte, bool>((byte)((s1 & AttribsMask) >> 7));
            voxel.Attribs[2] = UnsafeUtils.BitCast<byte, bool>((byte)((s2 & AttribsMask) >> 7));
            voxel.Attribs[3] = UnsafeUtils.BitCast<byte, bool>((byte)((s3 & AttribsMask) >> 7));
            voxel.Attribs[4] = UnsafeUtils.BitCast<byte, bool>((byte)((s4 & AttribsMask) >> 7));
            voxel.Attribs[5] = UnsafeUtils.BitCast<byte, bool>((byte)((s5 & AttribsMask) >> 7));

            destination[i] = voxel;
        }
    }

    /// <summary>
    /// Converts <see cref="Voxel"/>s to raw voxel data.
    /// </summary>
    /// <param name="voxels">The <see cref="Voxel"/>s to convert.</param>
    /// <returns>The converted raw voxel data.</returns>
    public static unsafe byte[] VoxelsToRaw(Voxel[] voxels)
    {
        ThrowIfNull(voxels, nameof(voxels));

        if (voxels.Length < NumbVoxels)
        {
            ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
        }

        byte[] result = new byte[NumbVoxels * 6];

        for (int i = 0; i < NumbVoxels; i++)
        {
            Voxel voxel = voxels[i];
            result[i + (NumbVoxels * 0)] = (byte)(voxel.Colors[0] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[0]) << 7);
            result[i + (NumbVoxels * 1)] = (byte)(voxel.Colors[1] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[1]) << 7);
            result[i + (NumbVoxels * 2)] = (byte)(voxel.Colors[2] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[2]) << 7);
            result[i + (NumbVoxels * 3)] = (byte)(voxel.Colors[3] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[3]) << 7);
            result[i + (NumbVoxels * 4)] = (byte)(voxel.Colors[4] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[4]) << 7);
            result[i + (NumbVoxels * 5)] = (byte)(voxel.Colors[5] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[5]) << 7);
        }

        return result;
    }

    /// <summary>
    /// Converts <see cref="Voxel"/>s to raw voxel data.
    /// </summary>
    /// <param name="voxels">The <see cref="Voxel"/>s to convert.</param>
    /// <param name="destination">The destination span.</param>
    public static unsafe void VoxelsToRaw(ReadOnlySpan<Voxel> voxels, Span<byte> destination)
    {
        if (voxels.Length < NumbVoxels)
        {
            ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
        }

        if (destination.Length < NumbVoxels * 6)
        {
            ThrowArgumentException($"{nameof(destination)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(destination));
        }

        for (int i = 0; i < NumbVoxels; i++)
        {
            Voxel voxel = voxels[i];
            destination[i + (NumbVoxels * 0)] = (byte)(voxel.Colors[0] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[0]) << 7);
            destination[i + (NumbVoxels * 1)] = (byte)(voxel.Colors[1] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[1]) << 7);
            destination[i + (NumbVoxels * 2)] = (byte)(voxel.Colors[2] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[2]) << 7);
            destination[i + (NumbVoxels * 3)] = (byte)(voxel.Colors[3] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[3]) << 7);
            destination[i + (NumbVoxels * 4)] = (byte)(voxel.Colors[4] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[4]) << 7);
            destination[i + (NumbVoxels * 5)] = (byte)(voxel.Colors[5] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[5]) << 7);
        }
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
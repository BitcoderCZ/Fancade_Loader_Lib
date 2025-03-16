// <copyright file="PrefabSegment.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;

namespace FancadeLoaderLib;

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

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabSegment"/> class.
	/// </summary>
	/// <param name="prefabId">Id of the prefab this segment is in.</param>
	/// <param name="posInPrefab">Position of this segment in prefab.</param>
	public PrefabSegment(ushort prefabId, byte3 posInPrefab)
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
	public PrefabSegment(ushort prefabId, byte3 posInPrefab, Voxel[]? voxels)
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
	public byte3 PosInPrefab { get; internal set; }

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
				ThrowHelper.ThrowArgumentException($"{nameof(Voxels)} must be {NumbVoxels} long, but {nameof(value)}.Length is {value.Length}.", nameof(value));
			}

			_voxels = value;
		}
	}

	/// <summary>
	/// Converts raw voxel data to <see cref="Voxel"/>s.
	/// </summary>
	/// <param name="voxels">The voxel data to convert.</param>
	/// <returns>The converted <see cref="Voxel"/>s.</returns>
	public static unsafe Voxel[] VoxelsFromRaw(byte[] voxels)
	{
		if (voxels is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(voxels));
		}

		if (voxels.Length < NumbVoxels * 6)
		{
			ThrowHelper.ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(voxels));
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
			ThrowHelper.ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(voxels));
		}

		if (destination.Length < NumbVoxels)
		{
			ThrowHelper.ThrowArgumentException($"{nameof(destination)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
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
		if (voxels is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(voxels));
		}

		if (voxels.Length < NumbVoxels)
		{
			ThrowHelper.ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
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
			ThrowHelper.ThrowArgumentException($"{nameof(voxels)}'s length must be greater than or equal to {NumbVoxels}.", nameof(voxels));
		}

		if (destination.Length < NumbVoxels * 6)
		{
			ThrowHelper.ThrowArgumentException($"{nameof(destination)}'s length must be greater than or equal to {NumbVoxels * 6}.", nameof(destination));
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
// <copyright file="Prefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib;

/// <summary>
/// Represents a prefab (block or level), processed for easier manipulation.
/// </summary>
public class Prefab : ICloneable
{
	/// <summary>
	/// The number of voxels in a prefab.
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

	/// <summary>
	/// The blocks inside this prefab.
	/// </summary>
	public readonly BlockData Blocks;

	/// <summary>
	/// Settings of the blocks inside this prefab.
	/// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
	public readonly List<PrefabSetting> Settings;

	/// <summary>
	/// Connections between blocks inside this prefab, block-block and block-outside of this prefab.
	/// </summary>
	public readonly List<Connection> Connections;
#pragma warning restore CA1002 // Do not expose generic lists

	/// <summary>
	/// The collider of this prefab.
	/// </summary>
	public PrefabCollider Collider;

	/// <summary>
	/// The type of this prefab.
	/// </summary>
	public PrefabType Type;

	/// <summary>
	/// The background color of this prefab.
	/// </summary>
	public FcColor BackgroundColor;

	/// <summary>
	/// If this prefab is editable.
	/// </summary>
	public bool Editable;

	/// <summary>
	/// Group id of this prefab if it is in a group; otherwise, <see cref="ushort.MaxValue"/>.
	/// </summary>
	public ushort GroupId;

	/// <summary>
	/// Position of this prefab in group.
	/// </summary>
	public byte3 PosInGroup;

	private string _name;

	private Voxel[]? _voxels;

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class.
	/// </summary>
	public Prefab()
	{
		_name = "New Block";
		BackgroundColor = FcColorUtils.DefaultBackgroundColor;
		Collider = PrefabCollider.Box;
		Type = PrefabType.Normal;
		Editable = true;
		GroupId = ushort.MaxValue;

		Blocks = new BlockData();
		Settings = [];
		Connections = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class.
	/// </summary>
	/// <param name="name">Name of this prefab.</param>
	/// <param name="collider">The collider of this prefab.</param>
	/// <param name="type">The type of this prefab.</param>
	/// <param name="backgroundColor">The background color of this prefab.</param>
	/// <param name="editable">If this prefab is editable.</param>
	/// <param name="voxels">Voxels/model of this prefab.</param>
	/// <param name="blocks">The blocks inside this prefab.</param>
	/// <param name="settings">Settings of the blocks inside this prefab.</param>
	/// <param name="connections">Connections between blocks inside this prefab, block-block and block-outside of this prefab.</param>
#pragma warning disable CA1002 // Do not expose generic lists
	public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
#pragma warning restore CA1002 // Do not expose generic lists
		: this(name, collider, type, backgroundColor, editable, ushort.MaxValue, default, voxels, blocks, settings, connections)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class.
	/// </summary>
	/// <param name="name">Name of this prefab.</param>
	/// <param name="collider">The collider of this prefab.</param>
	/// <param name="type">The type of this prefab.</param>
	/// <param name="backgroundColor">The background color of this prefab.</param>
	/// <param name="editable">If this prefab is editable.</param>
	/// <param name="groupId">Id of the group this prefab is in.</param>
	/// <param name="posInGroup">The position of this prefab in it's group.</param>
	/// <param name="voxels">Voxels/model of this prefab.</param>
	/// <param name="blocks">The blocks inside this prefab.</param>
	/// <param name="settings">Settings of the blocks inside this prefab.</param>
	/// <param name="connections">Connections between blocks inside this prefab, block-block and block-outside of this prefab.</param>
#pragma warning disable CA1002 // Do not expose generic lists
	public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, ushort groupId, byte3 posInGroup, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
#pragma warning restore CA1002 // Do not expose generic lists
	{
		_name = name;
		Collider = collider;
		Type = type;
		BackgroundColor = backgroundColor;
		Editable = editable;
		GroupId = groupId;
		PosInGroup = posInGroup;
		Voxels = voxels;
		Blocks = blocks ?? new BlockData();
		Settings = settings ?? [];
		Connections = connections ?? [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class.
	/// </summary>
	/// <param name="prefab">The prefab to copy.</param>
	public Prefab(Prefab prefab)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(prefab._name, prefab.Collider, prefab.Type, prefab.BackgroundColor, prefab.Editable, prefab.GroupId, prefab.PosInGroup, prefab.Voxels is null ? null : (Voxel[])prefab.Voxels.Clone(), prefab.Blocks.Clone(), [.. prefab.Settings], [.. prefab.Connections])
#pragma warning restore CA1062
	{
	}

	/// <summary>
	/// Gets or sets the name of the prefab.
	/// </summary>
	/// <remarks>
	/// Cannot be longer than 255 bytes when encoded as UTF8.
	/// </remarks>
	/// <value>Name of the prefab.</value>
	public string Name
	{
		get => _name;
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value), $"{nameof(Name)} cannot be null.");
			}

			_name = value;
		}
	}

	/// <summary>
	/// Gets a value indicating whether this prefab is in a group.
	/// </summary>
	/// <value><see langword="true"/> if this prifab is in a group; otherwise, <see langword="false"/>.</value>
	public bool IsInGroup => GroupId != ushort.MaxValue;

	/// <summary>
	/// Gets or sets the voxels/model of this prefab.
	/// </summary>
	/// <remarks>
	/// <para>Must be 8*8*8*6 (3072) long.</para>
	/// <para>The voxels are in XYZ order.</para>
	/// </remarks>
	/// <value>Voxels/model of this prefab.</value>
#pragma warning disable CA1819 // Properties should not return arrays
	public Voxel[]? Voxels
#pragma warning restore CA1819
	{
		get => _voxels;
		set
		{
			if (value is not null && value.Length != NumbVoxels)
			{
				throw new ArgumentException($"{nameof(Voxels)} must be {NumbVoxels} long, but {nameof(value)}.Length is {value.Length}.", nameof(value));
			}

			_voxels = value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class, with the default values for a block.
	/// </summary>
	/// <param name="name">Name of the prefab.</param>
	/// <returns>The new instance of <see cref="Prefab"/>.</returns>
	public static Prefab CreateBlock(string name)
	{
		Prefab prefab = new Prefab();

		prefab.Name = name;
		prefab._voxels = new Voxel[NumbVoxels];

		return prefab;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Prefab"/> class, with the default values for a level.
	/// </summary>
	/// <param name="name">Name of the prefab.</param>
	/// <returns>The new instance of <see cref="Prefab"/>.</returns>
	public static Prefab CreateLevel(string name)
	{
		Prefab prefab = new Prefab();

		prefab.Name = name;
		prefab.Collider = PrefabCollider.None;
		prefab.Type = PrefabType.Level;

		return prefab;
	}

	/// <summary>
	/// Converts <see cref="RawPrefab"/> into <see cref="Prefab"/>.
	/// </summary>
	/// <param name="rawPrefab">The <see cref="RawPrefab"/> to convert.</param>
	/// <param name="idOffset">The offset at which <paramref name="idOffsetAddition"/> starts to be applied.</param>
	/// <param name="idOffsetAddition">Added to group id, if the groupd id is >= <paramref name="idOffset"/>.</param>
	/// <param name="clone">If true clones Blocks, Settings and Connections else the values are assigned directly and <paramref name="rawPrefab"/> shouldn't be used anymore.</param>
	/// <returns>The converted <see cref="Prefab"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="rawPrefab"/> is invalid.</exception>
	public static unsafe Prefab FromRaw(RawPrefab rawPrefab, ushort idOffset, short idOffsetAddition, bool clone = true)
	{
		if (rawPrefab is null)
		{
			throw new ArgumentNullException(nameof(rawPrefab));
		}

		PrefabType type = PrefabType.Normal;
		if (rawPrefab.HasTypeByte)
		{
			type = (PrefabType)rawPrefab.TypeByte;
		}

		string name = "New Block";
		if (rawPrefab.NonDefaultName)
		{
			name = rawPrefab.Name;
		}

		FcColor backgroundColor = FcColorUtils.DefaultBackgroundColor;
		if (rawPrefab.NonDefaultBackgroundColor)
		{
			backgroundColor = (FcColor)rawPrefab.BackgroundColor;
		}

		bool editable = !rawPrefab.UnEditable && !rawPrefab.UnEditable2;

		PrefabCollider collider = PrefabCollider.Box;
		if (rawPrefab.HasColliderByte)
		{
			collider = (PrefabCollider)rawPrefab.ColliderByte;
		}

		ushort groupId = ushort.MaxValue;
		byte3 posInGroup = default;
		if (rawPrefab.IsInGroup)
		{
			groupId = rawPrefab.GroupId;

			if (idOffset <= rawPrefab.GroupId)
			{
				groupId = (ushort)(groupId + idOffsetAddition);
			}

			posInGroup = rawPrefab.PosInGroup;
		}

		Voxel[]? voxels = null;
		if (rawPrefab.HasVoxels)
		{
			if (rawPrefab.Voxels is null)
			{
				throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasVoxels)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Voxels)} is null", nameof(rawPrefab));
			}

			voxels = new Voxel[NumbVoxels];

			for (int i = 0; i < voxels.Length; i++)
			{
				Voxel voxel = default;
				byte s0 = rawPrefab.Voxels[i + (NumbVoxels * 0)];
				byte s1 = rawPrefab.Voxels[i + (NumbVoxels * 1)];
				byte s2 = rawPrefab.Voxels[i + (NumbVoxels * 2)];
				byte s3 = rawPrefab.Voxels[i + (NumbVoxels * 3)];
				byte s4 = rawPrefab.Voxels[i + (NumbVoxels * 4)];
				byte s5 = rawPrefab.Voxels[i + (NumbVoxels * 5)];

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

				voxels[i] = voxel;
			}
		}

		BlockData? blockData = null;
		if (rawPrefab.HasBlocks)
		{
			if (rawPrefab.Blocks is null)
			{
				throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasBlocks)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Blocks)} is null", nameof(rawPrefab));
			}

			ushort[] blocks = clone
				? (ushort[])rawPrefab.Blocks.Array.Clone()
				: rawPrefab.Blocks.Array;

			for (int i = 0; i < blocks.Length; i++)
			{
				if (idOffset <= blocks[i])
				{
					blocks[i] = (ushort)(blocks[i] + idOffsetAddition);
				}
			}

			blockData = new BlockData(new Array3D<ushort>(blocks, rawPrefab.Blocks.Size));
			blockData.Trim(false);
		}

		List<PrefabSetting>? settings = null;
		if (rawPrefab.HasSettings)
		{
			if (rawPrefab.Settings is null)
			{
				throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasSettings)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Settings)} is null", nameof(rawPrefab));
			}

			settings = clone
				? [.. rawPrefab.Settings]
				: rawPrefab.Settings;
		}

		// add settings to stock prefabs
		if (blockData is not null && blockData.Size != int3.Zero)
		{
			if (settings is null)
			{
				settings = [];
			}

			for (int i = 0; i < blockData.Array.Length; i++)
			{
				int id = blockData.Array[i];

				if (id != 0)
				{
					int numbStockSettings = 0; // TODO: getNumbStockSettings(id);
#pragma warning disable CA1508 // Avoid dead conditional code
					if (numbStockSettings != 0)
					{
						for (int setI = 0; setI < numbStockSettings; setI++)
						{
							ushort3 pos = (ushort3)blockData.Index(i);

							PrefabSetting setting = settings.FirstOrDefault(s => s.Index == setI && s.Position == pos);

							if (setting == default)
							{
								// Wasn't found
								// TODO: settings.Add(getStockSetting(id, setI));
							}
						}
					}
#pragma warning restore CA1508
				}
			}
		}

		List<Connection>? connections = null;
		if (rawPrefab.HasConnections)
		{
			if (rawPrefab.Connections is null)
			{
				throw new ArgumentException($"{nameof(rawPrefab)}.{nameof(RawPrefab.HasConnections)} is true, while {nameof(rawPrefab)}.{nameof(RawPrefab.Connections)} is null", nameof(rawPrefab));
			}

			connections = clone
				? [.. rawPrefab.Connections]
				: rawPrefab.Connections;
		}

		return new Prefab(name, collider, type, backgroundColor, editable, groupId, posInGroup, voxels, blockData, settings, connections);
	}

	/// <summary>
	/// Converts this <see cref="Prefab"/> into <see cref="RawPrefab"/>.
	/// </summary>
	/// <param name="clone">If the prefabs should be copied, if <see langword="true"/>, this <see cref="Game"/> instance shouldn't be used anymore.</param>
	/// <returns>A new instance of the <see cref="RawPrefab"/> class from this <see cref="game"/>.</returns>
	public unsafe RawPrefab ToRaw(bool clone)
	{
		byte[]? voxels = null;
		if (!(Voxels is null))
		{
			voxels = new byte[NumbVoxels * 6];

			for (int i = 0; i < NumbVoxels; i++)
			{
				Voxel voxel = Voxels[i];
				voxels[i + (NumbVoxels * 0)] = (byte)(voxel.Colors[0] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[0]) << 7);
				voxels[i + (NumbVoxels * 1)] = (byte)(voxel.Colors[1] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[1]) << 7);
				voxels[i + (NumbVoxels * 2)] = (byte)(voxel.Colors[2] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[2]) << 7);
				voxels[i + (NumbVoxels * 3)] = (byte)(voxel.Colors[3] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[3]) << 7);
				voxels[i + (NumbVoxels * 4)] = (byte)(voxel.Colors[4] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[4]) << 7);
				voxels[i + (NumbVoxels * 5)] = (byte)(voxel.Colors[5] | UnsafeUtils.BitCast<bool, byte>(voxel.Attribs[5]) << 7);
			}
		}

		Blocks.Trim();

		return new RawPrefab(
			hasConnections: !(Connections is null) && Connections.Count > 0,
			hasSettings: !(Settings is null) && Settings.Count > 0,
			hasBlocks: !(Blocks is null) && Blocks.Size != int3.Zero,
			hasVoxels: Type != PrefabType.Level && !(Voxels is null),
			isInGroup: GroupId != ushort.MaxValue,
			hasColliderByte: Collider != PrefabCollider.Box,
			unEditable: !Editable,
			unEditable2: !Editable,
			nonDefaultBackgroundColor: BackgroundColor != FcColorUtils.DefaultBackgroundColor,
			hasData2: false,
			hasData1: false,
			Name != "New Block",
			hasTypeByte: Type != 0,
			typeByte: (byte)Type,
			name: Name,
			data1: 0,
			data2: 0,
			backgroundColor: (byte)BackgroundColor,
			colliderByte: (byte)Collider,
			groupId: GroupId,
			posInGroup: PosInGroup,
			voxels: voxels,
			blocks: Blocks is null ? null : (clone ? Blocks.Array.Clone() : Blocks.Array),
			settings: clone && Settings is not null ? [.. Settings] : Settings,
			connections: clone && Connections is not null ? [.. Connections] : Connections);
	}

	/// <summary>
	/// Creates a deep copy of this <see cref="Prefab"/>.
	/// </summary>
	/// <returns>A deep copy of this <see cref="Prefab"/>.</returns>
	public Prefab Clone()
		=> new Prefab(this);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new Prefab(this);
}
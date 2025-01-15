// <copyright file="Prefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FancadeLoaderLib;

public class Prefab : ICloneable
{
	public const int NumbVoxels = 8 * 8 * 8;
	public const byte ColorMask = 0b_0111_1111;
	public const byte GlueMask = 0b_1000_0000;

	public static ImmutableArray<short3> SideToOffset =
	[
		new short3(1, 0, 0),
		new short3(-1, 0, 0),
		new short3(0, 1, 0),
		new short3(0, -1, 0),
		new short3(0, 0, 1),
		new short3(0, 0, -1),
	];

	public readonly BlockData Blocks;
	public readonly List<PrefabSetting> Settings;
	public readonly List<Connection> Connections;

	public PrefabCollider Collider;
	public PrefabType Type;
	public FcColor BackgroundColor;

	public bool Editable;

	public ushort GroupId;
	public byte3 PosInGroup;

	private string _name;

	private Voxel[]? _voxels;

	public Prefab()
	{
		_name = "New Block";
		BackgroundColor = FcColorE.Default;
		Collider = PrefabCollider.Box;
		Type = PrefabType.Normal;
		Editable = true;
		GroupId = ushort.MaxValue;

		Blocks = new BlockData();
		Settings = [];
		Connections = [];
	}

	public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
		: this(name, collider, type, backgroundColor, editable, ushort.MaxValue, default, voxels, blocks, settings, connections)
	{
	}

	public Prefab(string name, PrefabCollider collider, PrefabType type, FcColor backgroundColor, bool editable, ushort groupId, byte3 posInGroup, Voxel[]? voxels, BlockData? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
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

	public Prefab(Prefab prefab)
		: this(prefab._name, prefab.Collider, prefab.Type, prefab.BackgroundColor, prefab.Editable, prefab.GroupId, prefab.PosInGroup, prefab.Voxels is null ? null : (Voxel[])prefab.Voxels.Clone(), prefab.Blocks.Clone(), [.. prefab.Settings], [.. prefab.Connections])
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

	public bool IsInGroup => GroupId != ushort.MaxValue;

	public Voxel[]? Voxels
	{
		get => _voxels;
		set
		{
			if (!(value is null) && value.Length != NumbVoxels)
			{
				throw new ArgumentException($"{nameof(Voxels)} must be {NumbVoxels} long, but {nameof(value)}.Length is {value.Length}.", nameof(value));
			}

			_voxels = value;
		}
	}

	public static Prefab CreateBlock(string name)
	{
		Prefab prefab = new Prefab();

		prefab.Name = name;
		prefab._voxels = new Voxel[NumbVoxels];

		return prefab;
	}

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

		FcColor backgroundColor = FcColorE.Default;
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
				voxel.Attribs[0] = Unsafe.BitCast<byte, bool>((byte)((s0 & GlueMask) >> 7));
				voxel.Attribs[1] = Unsafe.BitCast<byte, bool>((byte)((s1 & GlueMask) >> 7));
				voxel.Attribs[2] = Unsafe.BitCast<byte, bool>((byte)((s2 & GlueMask) >> 7));
				voxel.Attribs[3] = Unsafe.BitCast<byte, bool>((byte)((s3 & GlueMask) >> 7));
				voxel.Attribs[4] = Unsafe.BitCast<byte, bool>((byte)((s4 & GlueMask) >> 7));
				voxel.Attribs[5] = Unsafe.BitCast<byte, bool>((byte)((s5 & GlueMask) >> 7));

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

			blockData = new BlockData(new Array3D<ushort>(blocks, rawPrefab.Blocks.LengthX, rawPrefab.Blocks.LengthY, rawPrefab.Blocks.LengthZ));
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
		if (!(blockData is null) && !(settings is null) && blockData.Length != 0)
		{
			for (int i = 0; i < blockData.Length; i++)
			{
				int id = blockData[i];

				if (id != 0)
				{
					int numbStockSettings = 0; // TODO: getNumbStockSettings(id);
					if (numbStockSettings != 0)
					{
						for (int setI = 0; setI < numbStockSettings; setI++)
						{
							ushort3 pos = (ushort3)blockData.Index(i);

							try
							{
								PrefabSetting setting = settings.First(s => s.Index == setI && s.Position == pos);
							}
							catch
							{
								// Wasn't found
								// TODO: settings.Add(getStockSetting(id, setI));
							}
						}
					}
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

	public unsafe RawPrefab ToRaw(bool clone)
	{
		byte[]? voxels = null;
		if (!(Voxels is null))
		{
			voxels = new byte[NumbVoxels * 6];

			for (int i = 0; i < NumbVoxels; i++)
			{
				Voxel voxel = Voxels[i];
				voxels[i + (NumbVoxels * 0)] = (byte)(voxel.Colors[0] | Unsafe.BitCast<bool, byte>(voxel.Attribs[0]) << 7);
				voxels[i + (NumbVoxels * 1)] = (byte)(voxel.Colors[1] | Unsafe.BitCast<bool,byte>(voxel.Attribs[1]) << 7);
				voxels[i + (NumbVoxels * 2)] = (byte)(voxel.Colors[2] | Unsafe.BitCast<bool,byte>(voxel.Attribs[2]) << 7);
				voxels[i + (NumbVoxels * 3)] = (byte)(voxel.Colors[3] | Unsafe.BitCast<bool,byte>(voxel.Attribs[3]) << 7);
				voxels[i + (NumbVoxels * 4)] = (byte)(voxel.Colors[4] | Unsafe.BitCast<bool,byte>(voxel.Attribs[4]) << 7);
				voxels[i + (NumbVoxels * 5)] = (byte)(voxel.Colors[5] | Unsafe.BitCast<bool,byte>(voxel.Attribs[5]) << 7);
			}
		}

		Blocks.Trim();

		return new RawPrefab(
			hasConnections: !(Connections is null) && Connections.Count > 0,
			hasSettings: !(Settings is null) && Settings.Count > 0,
			hasBlocks: !(Blocks is null) && Blocks.Length > 0,
			hasVoxels: Type != PrefabType.Level && !(Voxels is null),
			isInGroup: GroupId != ushort.MaxValue,
			hasColliderByte: Collider != PrefabCollider.Box,
			unEditable: !Editable,
			unEditable2: !Editable,
			nonDefaultBackgroundColor: BackgroundColor != FcColorE.Default,
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

	public Prefab Clone()
		=> new Prefab(this);

	object ICloneable.Clone()
		=> new Prefab(this);
}
// <copyright file="RawPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FancadeLoaderLib.Raw;

/// <summary>
/// Directly represents a prefab (block or level).
/// </summary>
public class RawPrefab
{
	#region Header

	/// <summary>
	/// <see langword="true"/> if the prefab has connection/wires inside it; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasConnections;

	/// <summary>
	/// <see langword="true"/> if the prefab has settings inside it; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasSettings;

	/// <summary>
	/// <see langword="true"/> if the prefab has blocks inside it; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasBlocks;

	/// <summary>
	/// <see langword="true"/> if the prefab has a model (voxels), true of all prefabs that aren't a level; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasVoxels;

	/// <summary>
	/// <see langword="true"/> if the prefab is in a group; otherwise, <see langword="false"/>.
	/// </summary>
	public bool IsInGroup;

	/// <summary>
	/// <see langword="true"/> if the prefab writes an additional byte to store the collider, used when the collider isn't <see cref="PrefabCollider.Box"/>; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasColliderByte;

	/// <summary>
	/// <see langword="true"/> if the prefab is not editable; otherwise, <see langword="false"/>.
	/// </summary>
	public bool UnEditable;

	/// <summary>
	/// <see langword="true"/> if the prefab is not editable, the editability of a prefab is determined with <see cref="UnEditable"/> || <see cref="UnEditable2"/>, possibly true for stock prefabs; otherwise, <see langword="false"/>.
	/// </summary>
	public bool UnEditable2;

	/// <summary>
	/// <see langword="true"/> if the prefab has a background color that isn't <see cref="FcColor.Blue"/>; otherwise, <see langword="false"/>.
	/// </summary>
	public bool NonDefaultBackgroundColor;

	/// <summary>
	/// <see langword="true"/> if the prefab has <see cref="Data2"/>; otherwise, <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Always <see langword="true"/> for the newest version.
	/// </remarks>
	public bool HasData2;

	/// <summary>
	/// <see langword="true"/> if the prefab has <see cref="Data1"/>; otherwise, <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Always <see langword="true"/> for the newest version.
	/// </remarks>
	public bool HasData1;

	/// <summary>
	/// <see langword="true"/> if the prefab's name isn't "New Block"; otherwise, <see langword="false"/>.
	/// </summary>
	public bool NonDefaultName;

	/// <summary>
	/// <see langword="true"/> if the prefab writes an additional byte to store it's type, used when the type isn't <see cref="PrefabType.Normal"/>; otherwise, <see langword="false"/>.
	/// </summary>
	public bool HasTypeByte;
	#endregion

	/// <summary>
	/// The type of this prefab, see <see cref="PrefabType"/>.
	/// </summary>
	public byte TypeByte;

	/// <summary>
	/// The name of this prefab.
	/// </summary>
	/// <remarks>
	/// Cannot be longer than 255 bytes when encoded as ASCII.
	/// </remarks>
	public string Name;

	/// <summary>
	/// Some data, unknown purpose.
	/// </summary>
	public byte Data1;

	/// <summary>
	/// Some data, unknown purpose.
	/// </summary>
	public uint Data2;

	/// <summary>
	/// The background color of this prefab, see <see cref="FcColor"/>.
	/// </summary>
	public byte BackgroundColor;

	/// <summary>
	/// The collider of this prefab, see <see cref="PrefabCollider"/>.
	/// </summary>
	public byte ColliderByte;

	/// <summary>
	/// Group id of this prefab if it is in a group; otherwise, <see cref="ushort.MaxValue"/>.
	/// </summary>
	public ushort GroupId;

	/// <summary>
	/// Position of this prefab in group.
	/// </summary>
	public byte3 PosInGroup;

	/// <summary>
	/// Voxels/model of this prefab.
	/// </summary>
	/// <remarks>
	/// <para>Must be 8*8*8*6 (3072) long.</para>
	/// <para>The first 512 bytes - info about the +X sides in XYZ order, then -X, +Y, -Y, +Z, -Z.</para>
	/// <para>First 7 bits - color, most significant bit - 1 if the side doesn't have glue, 1 if it does.</para>
	/// </remarks>
	public byte[]? Voxels;

	/// <summary>
	/// Ids of the blocks inside this prefab.
	/// </summary>
	public Array3D<ushort>? Blocks;

	/// <summary>
	/// Settings of the blocks inside this prefab.
	/// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
	public List<PrefabSetting>? Settings;
#pragma warning restore CA1002 // Do not expose generic lists

	/// <summary>
	/// Connections between blocks inside this prefab, block-block and block-outside of this prefab.
	/// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
	public List<Connection>? Connections;
#pragma warning restore CA1002 // Do not expose generic lists

	/// <summary>
	/// Initializes a new instance of the <see cref="RawPrefab"/> class.
	/// </summary>
	public RawPrefab()
	{
		Name = string.Empty;
	}

#pragma warning disable SA1625 // Element documentation should not be copied and pasted
	/// <summary>
	/// Initializes a new instance of the <see cref="RawPrefab"/> class.
	/// </summary>
	/// <param name="hasConnections"><see langword="true"/> if the prefab has connection/wires inside it; otherwise, <see langword="false"/>.</param>
	/// <param name="hasSettings"><see langword="true"/> if the prefab has settings inside it; otherwise, <see langword="false"/>.</param>
	/// <param name="hasBlocks"><see langword="true"/> if the prefab has blocks inside it; otherwise, <see langword="false"/>.</param>
	/// <param name="hasVoxels"><see langword="true"/> if the prefab has a model (voxels), true of all prefabs that aren't a level; otherwise, <see langword="false"/>.</param>
	/// <param name="isInGroup"><see langword="true"/> if the prefab is in a group; otherwise, <see langword="false"/>.</param>
	/// <param name="hasColliderByte"><see langword="true"/> if the prefab writes an additional byte to store the collider, used when the collider isn't <see cref="PrefabCollider.Box"/>; otherwise, <see langword="false"/>.</param>
	/// <param name="unEditable"><see langword="true"/> if the prefab is not editable; otherwise, <see langword="false"/>.</param>
	/// <param name="unEditable2"> <see langword="true"/> if the prefab is not editable, the editability of a prefab is determined with <see cref="UnEditable"/> || <see cref="UnEditable2"/>, possibly true for stock prefabs; otherwise, <see langword="false"/>.</param>
	/// <param name="nonDefaultBackgroundColor"><see langword="true"/> if the prefab has a background color that isn't <see cref="FcColor.Blue"/>; otherwise, <see langword="false"/>.</param>
	/// <param name="hasData2">Always <see langword="true"/> for the newest version.</param>
	/// <param name="hasData1">Always <see langword="true"/> for the newest version.</param>
	/// <param name="nonDefaultName"><see langword="true"/> if the prefab's name isn't "New Block"; otherwise, <see langword="false"/>.</param>
	/// <param name="hasTypeByte"><see langword="true"/> if the prefab writes an additional byte to store it's type, used when the type isn't <see cref="PrefabType.Normal"/>; otherwise, <see langword="false"/>.</param>
	/// <param name="typeByte">The type of this prefab, see <see cref="PrefabType"/>.</param>
	/// <param name="name">The name of this prefab.</param>
	/// <param name="data1">Some data, unknown purpose.</param>
	/// <param name="data2">Some data, unknown purpose.</param>
	/// <param name="backgroundColor">The background color of this prefab.</param>
	/// <param name="colliderByte">The collider of this prefab, see <see cref="PrefabCollider"/>.</param>
	/// <param name="groupId">Group id of this prefab if it is in a group; otherwise, <see cref="ushort.MaxValue"/>.</param>
	/// <param name="posInGroup">Position of this prefab in group.</param>
	/// <param name="voxels">Voxels/model of this prefab, must be 8*8*8*6 (3072) long.</param>
	/// <param name="blocks">Ids of the blocks inside this prefab.</param>
	/// <param name="settings">Settings of the blocks inside this prefab.</param>
	/// <param name="connections">Connections between blocks inside this prefab, block-block and block-outside of this prefab.</param>
#pragma warning disable CA1002 // Do not expose generic lists
	public RawPrefab(bool hasConnections, bool hasSettings, bool hasBlocks, bool hasVoxels, bool isInGroup, bool hasColliderByte, bool unEditable, bool unEditable2, bool nonDefaultBackgroundColor, bool hasData2, bool hasData1, bool nonDefaultName, bool hasTypeByte, byte typeByte, string name, byte data1, uint data2, byte backgroundColor, byte colliderByte, ushort groupId, byte3 posInGroup, byte[]? voxels, Array3D<ushort>? blocks, List<PrefabSetting>? settings, List<Connection>? connections)
#pragma warning restore CA1002 // Do not expose generic lists
#pragma warning restore SA1625 // Element documentation should not be copied and pasted
	{
		HasConnections = hasConnections;
		HasSettings = hasSettings;
		HasBlocks = hasBlocks;
		HasVoxels = hasVoxels;
		IsInGroup = isInGroup;
		HasColliderByte = hasColliderByte;
		UnEditable = unEditable;
		UnEditable2 = unEditable2;
		NonDefaultBackgroundColor = nonDefaultBackgroundColor;
		HasData2 = hasData2;
		HasData1 = hasData1;
		NonDefaultName = nonDefaultName;
		HasTypeByte = hasTypeByte;
		TypeByte = typeByte;
		Name = name;
		Data1 = data1;
		Data2 = data2;
		BackgroundColor = backgroundColor;
		ColliderByte = colliderByte;
		GroupId = groupId;
		PosInGroup = posInGroup;
		Voxels = voxels;
		Blocks = blocks;
		Settings = settings;
		Connections = connections;
	}

	/// <summary>
	/// Loads a <see cref="RawPrefab"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="RawPrefab"/> from.</param>
	/// <returns>A <see cref="RawPrefab"/> read from <paramref name="reader"/>.</returns>
	public static unsafe RawPrefab Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			throw new ArgumentNullException(nameof(reader));
		}

		byte header0 = reader.ReadUInt8();
		byte header1 = reader.ReadUInt8();

		bool hasConnections = (header0 & 1) == 1;
		bool hasSettings = ((header0 >> 1) & 1) == 1;
		bool hasBlocks = ((header0 >> 2) & 1) == 1;
		bool hasVoxels = ((header0 >> 3) & 1) == 1;
		bool isInGroup = ((header0 >> 4) & 1) == 1;
		bool hasColliderByte = ((header0 >> 5) & 1) == 1;
		bool unEditable = ((header0 >> 6) & 1) == 1;
		bool unEditable2 = (header0 >> 7) == 1;
		bool nonDefaultBackgroundColor = (header1 & 1) == 1;
		bool hasData2 = ((header1 >> 1) & 1) == 1;
		bool hasData1 = ((header1 >> 2) & 1) == 1;
		bool nonDefaultName = ((header1 >> 3) & 1) == 1;
		bool hasTypeByte = ((header1 >> 4) & 1) == 1;

		byte typeByte = (byte)PrefabType.Normal;
		if (hasTypeByte)
		{
			typeByte = reader.ReadUInt8();
		}

		string name = "New Block";
		if (nonDefaultName)
		{
			name = reader.ReadString();
		}

		byte data1 = 0;
		if (hasData1)
		{
			data1 = reader.ReadUInt8();
		}

		uint data2 = 0;
		if (hasData2)
		{
			data2 = reader.ReadUInt32();
		}

		byte backgroundColor = (byte)FcColorUtils.DefaultBackgroundColor;
		if (nonDefaultBackgroundColor)
		{
			backgroundColor = reader.ReadUInt8();
		}

		byte colliderByte = (byte)PrefabCollider.Box;
		if (hasColliderByte)
		{
			colliderByte = reader.ReadUInt8();
		}

		ushort groupId = 0;
		byte3 posInGroup = default;
		if (isInGroup)
		{
			groupId = reader.ReadUInt16();
			posInGroup = reader.ReadVec3B();
		}

		byte[]? voxels = null;
		if (hasVoxels)
		{
			// size (8*8*8) * sides (6)
			voxels = reader.ReadBytes(8 * 8 * 8 * 6);
		}

		ushort3 insideSize = default;
		ushort[]? blocks = null;
		if (hasBlocks)
		{
			insideSize = reader.ReadVec3US();

			int insideLen = insideSize.X * insideSize.Y * insideSize.Z;

			if (insideLen == 0)
			{
				blocks = [];
			}
			else
			{
				blocks = new ushort[insideLen];

				if (BitConverter.IsLittleEndian)
				{
					reader.ReadSpan(MemoryMarshal.Cast<ushort, byte>(blocks.AsSpan()));
				}
				else
				{
					int byteLength = insideLen * sizeof(ushort);
					Span<byte> blockBytes = byteLength < 1024 ? stackalloc byte[byteLength] : new byte[byteLength];

					reader.ReadSpan(blockBytes);

					for (int i = 0; i < insideLen; i++)
					{
						blocks[i] = (ushort)(blockBytes[i * 2] | (blockBytes[(i * 2) + 1] << 8));
					}
				}
			}
		}

		ushort numbSettings = 0;
		List<PrefabSetting>? settings = null;
		if (hasSettings)
		{
			numbSettings = reader.ReadUInt16();

			if (numbSettings == 0)
			{
				settings = [];
			}
			else
			{
				settings = new List<PrefabSetting>(numbSettings);

				for (int i = 0; i < numbSettings; i++)
				{
					settings.Add(PrefabSetting.Load(reader));
				}
			}
		}

		ushort numbConnections = 0;
		List<Connection>? connections = null;
		if (hasConnections)
		{
			numbConnections = reader.ReadUInt16();

			if (numbConnections == 0)
			{
				connections = [];
			}
			else
			{
				connections = new List<Connection>(numbConnections);

				for (int i = 0; i < numbConnections; i++)
				{
					connections.Add(Connection.Load(reader));
				}
			}
		}

		return new RawPrefab(hasConnections, hasSettings, hasBlocks, hasVoxels, isInGroup, hasColliderByte, unEditable, unEditable2, nonDefaultBackgroundColor, hasData2, hasData1, nonDefaultName, hasTypeByte, typeByte, name, data1, data2, backgroundColor, colliderByte, groupId, posInGroup, voxels, blocks is null ? null : new Array3D<ushort>(blocks, insideSize.X, insideSize.Y, insideSize.Z), settings, connections);
	}

	/// <summary>
	/// Writes a <see cref="RawPrefab"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public unsafe void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		ushort header = 0;

		if (HasTypeByte)
		{
			header |= 0b0001000000000000;
		}

		if (NonDefaultName)
		{
			header |= 0b100000000000;
		}

		if (NonDefaultBackgroundColor)
		{
			header |= 0b100000000;
		}

		if (UnEditable2)
		{
			header |= 0b10000000;
		}

		if (UnEditable)
		{
			header |= 0b1000000;
		}

		if (HasColliderByte)
		{
			header |= 0b100000;
		}

		if (IsInGroup)
		{
			header |= 0b10000;
		}

		if (HasVoxels)
		{
			header |= 0b1000;
		}

		if (HasBlocks)
		{
			header |= 0b100;
		}

		if (HasSettings)
		{
			header |= 0b10;
		}

		if (HasConnections)
		{
			header |= 0b1;
		}

		writer.WriteUInt16(header);

		if (HasTypeByte)
		{
			writer.WriteUInt8(TypeByte);
		}

		if (NonDefaultName)
		{
			writer.WriteString(Name);
		}

		if (NonDefaultBackgroundColor)
		{
			writer.WriteUInt8(BackgroundColor);
		}

		if (HasColliderByte)
		{
			writer.WriteUInt8(ColliderByte);
		}

		if (IsInGroup)
		{
			writer.WriteUInt16(GroupId);
			writer.WriteByte3(PosInGroup);
		}

		if (HasVoxels)
		{
			writer.WriteBytes(Voxels!);
		}

		if (HasBlocks)
		{
			writer.WriteUshort3(new ushort3(Blocks!.LengthX, Blocks!.LengthY, Blocks!.LengthZ));

			if (BitConverter.IsLittleEndian)
			{
				writer.WriteSpan(MemoryMarshal.Cast<ushort, byte>(Blocks!.Array.AsSpan()));
			}
			else
			{
				const int Mask1 = byte.MaxValue;
				const int Mask2 = byte.MaxValue << 8;

				ushort[] blocks = Blocks!.Array;
				byte[] blockBytes = new byte[blocks.Length * sizeof(ushort)];

				for (int i = 0; i < blocks.Length; i++)
				{
					blockBytes[(i * 2) + 0] = (byte)((blocks[i] & Mask1) >> 0);
					blockBytes[(i * 2) + 1] = (byte)((blocks[i] & Mask2) >> 8);
				}
			}
		}

		if (HasSettings)
		{
			writer.WriteUInt16((ushort)Settings!.Count);

			for (int i = 0; i < Settings!.Count; i++)
			{
				Settings[i]!.Save(writer);
			}
		}

		if (HasConnections)
		{
			writer.WriteUInt16((ushort)Connections!.Count);

			for (int i = 0; i < Connections!.Count; i++)
			{
				Connections[i]!.Save(writer);
			}
		}
	}
}
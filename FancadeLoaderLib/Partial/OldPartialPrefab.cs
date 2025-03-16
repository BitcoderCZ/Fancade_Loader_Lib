// <copyright file="OldPartialPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using static FancadeLoaderLib.Utils.ThrowHelper;

namespace FancadeLoaderLib.Partial;

internal readonly struct OldPartialPrefab
{
	/// <summary>
	/// Type of the prefab.
	/// </summary>
	public readonly PrefabType Type;

	/// <summary>
	/// Id of the group this prefab is in or <see cref="ushort.MaxValue"/> if it isn't in a group.
	/// </summary>
	public readonly ushort GroupId;

	/// <summary>
	/// Position of this prefab in it's group, if it is in one.
	/// </summary>
	public readonly byte3 PosInGroup;

	private readonly string _name;

	/// <summary>
	/// Initializes a new instance of the <see cref="OldPartialPrefab"/> struct.
	/// </summary>
	/// <param name="name">The name of this prefab.</param>
	/// <param name="type">The type of this prefab.</param>
	/// <param name="groupId">Id of the group this prefab is in or <see cref="ushort.MaxValue"/> if it isn't in a group.</param>
	/// <param name="posInGroup">Position of this prefab in it's group, if it is in one.</param>
	public OldPartialPrefab(string name, PrefabType type, ushort groupId, byte3 posInGroup)
	{
		if (string.IsNullOrEmpty(name))
		{
			ThrowArgumentException(nameof(name));
		}

		_name = name;
		Type = type;
		GroupId = groupId;
		PosInGroup = posInGroup;
	}

	/// <summary>
	/// Gets the name of this prefab.
	/// </summary>
	/// <value>The name of this prefab.</value>
	public readonly string Name => _name;

	/// <summary>
	/// Gets a value indicating whether this prifab is in a group.
	/// </summary>
	/// <value><see langword="true"/> if this prefab is in a group; otherwise, <see langword="false"/>.</value>
	public bool IsInGroup => GroupId != ushort.MaxValue;

	/// <summary>
	/// Loads a <see cref="OldPartialPrefab"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="OldPartialPrefab"/> from.</param>
	/// <returns>A <see cref="OldPartialPrefab"/> read from <paramref name="reader"/>.</returns>
	public static OldPartialPrefab Load(FcBinaryReader reader)
	{
		ThrowIfNull(reader, nameof(reader));

		byte header = reader.ReadUInt8();

		bool hasTypeByte = ((header >> 0) & 1) == 1;
		bool nonDefaultName = ((header >> 1) & 1) == 1;
		bool isInGroup = ((header >> 2) & 1) == 1;

		PrefabType type = PrefabType.Normal;
		if (hasTypeByte)
		{
			type = (PrefabType)reader.ReadUInt8();
		}

		string name = "New Block";
		if (nonDefaultName)
		{
			name = reader.ReadString();
		}

		ushort groupId = ushort.MaxValue;
		byte3 posInGroup = default;
		if (isInGroup)
		{
			groupId = reader.ReadUInt16();
			posInGroup = reader.ReadVec3B();
		}

		return new OldPartialPrefab(name, type, groupId, posInGroup);
	}

	/// <summary>
	/// Writes a <see cref="OldPartialPrefab"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public void Save(FcBinaryWriter writer)
	{
		ThrowIfNull(writer, nameof(writer));

		byte header = 0;

		if (Type != PrefabType.Normal)
		{
			header |= 0b1;
		}

		if (Name != "New Block")
		{
			header |= 0b10;
		}

		if (IsInGroup)
		{
			header |= 0b100;
		}

		writer.WriteUInt8(header);

		if (Type != PrefabType.Normal)
		{
			writer.WriteUInt8((byte)Type);
		}

		if (Name != "New Block")
		{
			writer.WriteString(Name);
		}

		if (IsInGroup)
		{
			writer.WriteUInt16(GroupId);
			writer.WriteByte3(PosInGroup);
		}
	}
}

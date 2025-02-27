// <copyright file="PartialPrefab.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// Only the name, type and group info of <see cref="Prefab"/>, used by <see cref="PartialPrefabGroup"/>.
/// </summary>
public class PartialPrefab : ICloneable
{
	/// <summary>
	/// Type of the prefab.
	/// </summary>
	public PrefabType Type;

	/// <summary>
	/// Id of the group this prefab is in or <see cref="ushort.MaxValue"/> if it isn't in a group.
	/// </summary>
	public ushort GroupId;

	/// <summary>
	/// Position of this prefab in it's group, if it is in one.
	/// </summary>
	public byte3 PosInGroup;

	private string _name;

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefab"/> class.
	/// </summary>
	/// <param name="other">The <see cref="PartialPrefab"/> to copy values from.</param>
	public PartialPrefab(PartialPrefab other)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(other._name, other.Type, other.GroupId, other.PosInGroup)
#pragma warning restore CA1062 // Validate arguments of public methods
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefab"/> class.
	/// </summary>
	/// <param name="other">The <see cref="Prefab"/> to copy values from.</param>
	public PartialPrefab(Prefab other)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(other.Name, other.Type, other.GroupId, other.PosInGroup)
#pragma warning restore CA1062 // Validate arguments of public methods
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefab"/> class.
	/// </summary>
	/// <param name="other">The <see cref="RawPrefab"/> to copy values from.</param>
	public PartialPrefab(RawPrefab other)
#pragma warning disable CA1062 // Validate arguments of public methods
		: this(other.Name, other.HasTypeByte ? (PrefabType)other.TypeByte : PrefabType.Normal, other.GroupId, other.PosInGroup)
#pragma warning restore CA1062 // Validate arguments of public methods
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefab"/> class.
	/// </summary>
	/// <param name="name">The name of this prefab.</param>
	/// <param name="type">The type of this prefab.</param>
	public PartialPrefab(string name, PrefabType type)
		: this(name, type, ushort.MaxValue, default)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PartialPrefab"/> class.
	/// </summary>
	/// <param name="name">The name of this prefab.</param>
	/// <param name="type">The type of this prefab.</param>
	/// <param name="groupid">Id of the group this prefab is in or <see cref="ushort.MaxValue"/> if it isn't in a group.</param>
	/// <param name="posInGroup">Position of this prefab in it's group, if it is in one.</param>
	public PartialPrefab(string name, PrefabType type, ushort groupid, byte3 posInGroup)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException(nameof(name));
		}

		_name = name;
		Type = type;
		GroupId = groupid;
		PosInGroup = posInGroup;
	}

	/// <summary>
	/// Gets or sets the name of this prefab.
	/// </summary>
	/// <value>The name of this prefab.</value>
	public string Name
	{
		get => _name;
		set
		{
			if (value is null)
			{
				ThrowHelper.ThrowArgumentNull(nameof(value), $"{nameof(Name)} cannot be null.");
			}

			_name = value;
		}
	}

	/// <summary>
	/// Gets a value indicating whether this prifab is in a group.
	/// </summary>
	/// <value><see langword="true"/> if this prefab is in a group; otherwise, <see langword="false"/>.</value>
	public bool IsInGroup => GroupId != ushort.MaxValue;

	/// <summary>
	/// Loads a <see cref="PartialPrefab"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="PartialPrefab"/> from.</param>
	/// <returns>A <see cref="PartialPrefab"/> read from <paramref name="reader"/>.</returns>
	public static PartialPrefab Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowHelper.ThrowArgumentNull(nameof(reader));
		}

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

		return new PartialPrefab(name, type, groupId, posInGroup);
	}

	/// <summary>
	/// Writes a <see cref="PartialPrefab"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowHelper.ThrowArgumentNull(nameof(writer));
		}

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

	/// <summary>
	/// Creates a copy of this <see cref="PartialPrefab"/>.
	/// </summary>
	/// <returns>A copy of this <see cref="PartialPrefab"/>.</returns>
	public PartialPrefab Clone()
		=> new PartialPrefab(this);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefab(this);
}

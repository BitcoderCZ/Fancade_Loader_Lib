// <copyright file="PartialPrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static FancadeLoaderLib.Utils.ThrowHelper;

#pragma warning disable CA1716
namespace FancadeLoaderLib.Partial;
#pragma warning restore CA1716

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="PartialPrefabGroup"/> manipulation.
/// </summary>
/// <remarks>
/// <para>Allows for saving/loading.</para>
/// </remarks>
public class PartialPrefabList : ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly Dictionary<ushort, PartialPrefabGroup> _groups;

	public PartialPrefabList()
	{
		_groups = [];
	}

	public PartialPrefabList(int caapcity)
	{
		_groups = new(caapcity);
	}

	public PartialPrefabList(IEnumerable<PartialPrefabGroup> collection)
	{
		if (collection is null)
		{
			ThrowArgumentNullException(nameof(collection));
		}

		_groups = collection.ToDictionary(group => group.Id);
		ValidateGroups(_groups.Values, nameof(collection)); // validate using _groups.Values to avoid iterating over collection multiple times

		IdOffset = _groups.Min(item => item.Key);
	}

	public PartialPrefabList(PartialPrefabList list, bool deepCopy)
	{
		ThrowIfNull(list, nameof(list));

		if (deepCopy)
		{
			_groups = list._groups.ToDictionary(item => item.Key, item => item.Value.Clone());
		}
		else
		{
			_groups = new(list._groups);
		}
	}

	private PartialPrefabList(Dictionary<ushort, PartialPrefabGroup> dict)
	{
		_groups = dict;
	}

	public int GroupCount => _groups.Count;

	public int PrefabCount => _groups.Sum(item => item.Value.Count);

	public IEnumerable<PartialPrefabGroup> Groups => _groups.Values;

	public static PartialPrefabList Load(FcBinaryReader reader)
	{
		ThrowIfNull(reader, nameof(reader));

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		PartialPrefab[] prefabs = new PartialPrefab[count];

		for (int i = 0; i < count; i++)
		{
			prefabs[i] = PartialPrefab.Load(reader);
		}

		Dictionary<ushort, PartialPrefabGroup> groups = [];

		for (int i = 0; i < prefabs.Length; i++)
		{
			if (prefabs[i].IsInGroup)
			{
				int startIndex = i;
				ushort groupId = prefabs[i].GroupId;
				do
				{
					i++;
				} while (i < count && prefabs[i].GroupId == groupId);

				ushort id = (ushort)(startIndex + idOffset);
				var prefab = prefabs[startIndex];
				groups.Add(id, new PartialPrefabGroup(id, prefab.Name, prefab.Type, prefabs.Skip(startIndex).Take(i - startIndex).Select(prefab => prefab.PosInGroup)));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + idOffset);
				var prefab = prefabs[i];
				groups.Add(id, new PartialPrefabGroup(id, prefab.Name, prefab.Type, [byte3.Zero]));
			}
		}

		return new PartialPrefabList(groups)
		{
			IdOffset = idOffset,
		};
	}

	public void Save(FcBinaryWriter writer)
	{
		ThrowIfNull(writer, nameof(writer));

		int prefabCount = PrefabCount;

		writer.WriteUInt32((uint)PrefabCount);
		writer.WriteUInt16(IdOffset);

		foreach (var (_, group) in _groups.OrderBy(item => item.Key))
		{
			int i = 0;
			foreach (var pos in group)
			{
				new PartialPrefab(i++ == 0 ? group.Name : "New Block", group.Type, group.Count > 1 ? group.Id : ushort.MaxValue, pos).Save(writer);
			}
		}
	}

	public PartialPrefabGroup GetGroup(ushort id)
		=> _groups[id];

	public bool TryGetGroup(ushort id, [MaybeNullWhen(false)] out PartialPrefabGroup group)
		=> _groups.TryGetValue(id, out group);

	public void AddGroup(PartialPrefabGroup group)
	{
		if (group.Id != PrefabCount + IdOffset)
		{
			ThrowArgumentException($"{group.Id} must be equal to {nameof(PrefabCount)} + {nameof(IdOffset)}.", nameof(group));
		}

		_groups.Add(group.Id, group);
	}

	public void InsertGroup(PartialPrefabGroup group)
	{
		if (WillBeLastGroup(group))
		{
			AddGroup(group);
			return;
		}

		if (!_groups.ContainsKey(group.Id))
		{
			ThrowArgumentException($"{nameof(_groups)} must contain {nameof(group)}.{nameof(PrefabGroup.Id)}.", nameof(group));
		}

		IncreaseAfter(group.Id, (ushort)group.Count);
		_groups.Add(group.Id, group);
	}

	public bool RemoveGroup(ushort id)
	{
		if (!_groups.Remove(id, out var group))
		{
			return false;
		}

		if (IsLastGroup(group))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)group.Count);

		return true;
	}

	public PartialPrefabList Clone(bool deepCopy)
		=> new PartialPrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PartialPrefabList(this, true);

	private static void ValidateGroups(IEnumerable<PartialPrefabGroup> groups, string paramName)
	{
		int? nextId = null;

		foreach (var group in groups.OrderBy(group => group.Id))
		{
			if (nextId == null || group.Id == nextId)
			{
				nextId = group.Id + group.Count;
			}
			else
			{
				throw new ArgumentException($"Groups in {paramName} must have consecutive IDs. Expected ID {nextId}, but found {group.Id}.", paramName);
			}
		}
	}

	private bool IsLastGroup(PartialPrefabGroup group)
		=> group.Id + group.Count >= PrefabCount + IdOffset;

	private bool WillBeLastGroup(PartialPrefabGroup group)
		=> group.Id >= PrefabCount + IdOffset;

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		List<ushort> groupsToChangeId = [];

		foreach (var (id, group) in _groups)
		{
			if (id >= index)
			{
				groupsToChangeId.Add(id);
			}
		}

		foreach (ushort id in groupsToChangeId.OrderByDescending(item => item))
		{
			bool removed = _groups.Remove(id, out var group);

			Debug.Assert(removed, "Group should have been removed.");
			Debug.Assert(group is not null, $"{group} shouldn't be null.");

			ushort newId = (ushort)(id + amount);
			group.Id = newId;
			_groups[newId] = group;
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		List<ushort> groupsToChangeId = [];

		foreach (var (id, group) in _groups)
		{
			if (id >= index)
			{
				groupsToChangeId.Add(id);
			}
		}

		foreach (ushort id in groupsToChangeId.OrderBy(item => item))
		{
			bool removed = _groups.Remove(id, out var group);

			Debug.Assert(removed, "Group should have been removed.");
			Debug.Assert(group is not null, $"{group} shouldn't be null.");

			ushort newId = (ushort)(id - amount);
			group.Id = newId;
			_groups[newId] = group;
		}
	}

	private readonly struct PartialPrefab
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
		/// Initializes a new instance of the <see cref="PartialPrefab"/> struct.
		/// </summary>
		/// <param name="name">The name of this prefab.</param>
		/// <param name="type">The type of this prefab.</param>
		/// <param name="groupid">Id of the group this prefab is in or <see cref="ushort.MaxValue"/> if it isn't in a group.</param>
		/// <param name="posInGroup">Position of this prefab in it's group, if it is in one.</param>
		public PartialPrefab(string name, PrefabType type, ushort groupid, byte3 posInGroup)
		{
			if (string.IsNullOrEmpty(name))
			{
				ThrowArgumentException(nameof(name));
			}

			_name = name;
			Type = type;
			GroupId = groupid;
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
		/// Loads a <see cref="PartialPrefab"/> from a <see cref="FcBinaryReader"/>.
		/// </summary>
		/// <param name="reader">The reader to read the <see cref="PartialPrefab"/> from.</param>
		/// <returns>A <see cref="PartialPrefab"/> read from <paramref name="reader"/>.</returns>
		public static PartialPrefab Load(FcBinaryReader reader)
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

			return new PartialPrefab(name, type, groupId, posInGroup);
		}

		/// <summary>
		/// Writes a <see cref="PartialPrefab"/> into a <see cref="FcBinaryWriter"/>.
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
}

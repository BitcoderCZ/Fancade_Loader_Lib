// <copyright file="PrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using FancadeLoaderLib.Utils;
using MathUtils.Vectors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace FancadeLoaderLib;

/// <summary>
/// <see cref="List{T}"/> wrapper for easier <see cref="PrefabGroup"/> manipulation.
/// </summary>
/// <remarks>
/// Group ids are automatically changed when prefabs are inserter/removed.
/// <para>Allows for saving/loading.</para>
/// </remarks>
public class PrefabList : ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly Dictionary<ushort, PrefabGroup> _groups;
	internal readonly List<Prefab> _prefabs;

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	public PrefabList()
	{
		_groups = [];
		_prefabs = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <param name="capacity">The initial group capacity.</param>
	public PrefabList(int capacity)
	{
		_groups = new(capacity);
		_prefabs = new(capacity);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <remarks>Sets <see cref="IdOffset"/> to the lowest <see cref="PrefabGroup.Id"/>.</remarks>
	/// <param name="collection">The groups to place into this list.</param>
	public PrefabList(IEnumerable<PrefabGroup> collection)
	{
		if (collection is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(collection));
		}

		_groups = collection.ToDictionary(group => group.Id);
		_prefabs = [.. PrefabsFromGroups(_groups)];

		IdOffset = _groups.Min(item => item.Key);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PrefabList"/> class.
	/// </summary>
	/// <param name="list">The <see cref="PrefabList"/> to copy values from.</param>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	public PrefabList(PrefabList list, bool deepCopy)
	{
		if (deepCopy)
		{
			_groups = list._groups.ToDictionary(item => item.Key, item => item.Value.Clone(true));
			_prefabs = [.. PrefabsFromGroups(_groups)];
		}
		else
		{
			_groups = new(list._groups);
			_prefabs = [.. PrefabsFromGroups(_groups)];
		}
	}

	private PrefabList(Dictionary<ushort, PrefabGroup> dict)
	{
		_groups = dict;
		_prefabs = [.. PrefabsFromGroups(_groups)];
	}

	public int GroupCount => _groups.Count;

	public int PrefabCount => _prefabs.Count;

	public Prefab this[int index]
	{
		get => _prefabs[index];
		set => _prefabs[index] = value;
	}

	/// <summary>
	/// Loads a <see cref="PrefabList"/> from a <see cref="FcBinaryReader"/>.
	/// </summary>
	/// <param name="reader">The reader to read the <see cref="PrefabList"/> from.</param>
	/// <returns>A <see cref="PrefabList"/> read from <paramref name="reader"/>.</returns>
	public static PrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(reader));
		}

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		RawPrefab[] rawPrefabs = new RawPrefab[count];

		for (int i = 0; i < count; i++)
		{
			rawPrefabs[i] = RawPrefab.Load(reader);
		}

		Dictionary<ushort, PrefabGroup> groups = [];

		for (int i = 0; i < rawPrefabs.Length; i++)
		{
			if (rawPrefabs[i].IsInGroup)
			{
				int startIndex = i;
				ushort groupId = rawPrefabs[i].GroupId;
				do
				{
					i++;
				} while (rawPrefabs[i].GroupId == groupId);

				ushort id = (ushort)(startIndex + RawGame.CurrentNumbStockPrefabs);
				groups.Add(id, PrefabGroup.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex), ushort.MaxValue, 0, false));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + RawGame.CurrentNumbStockPrefabs);
				groups.Add(id, PrefabGroup.FromRaw(id, [rawPrefabs[i]], ushort.MaxValue, 0, false));
			}
		}

		return new PrefabList(groups)
		{
			IdOffset = idOffset,
		};
	}

	/// <summary>
	/// Writes a <see cref="PrefabList"/> into a <see cref="FcBinaryWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="FcBinaryWriter"/> to write this instance into.</param>
	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowHelper.ThrowArgumentNullException(nameof(writer));
		}

		writer.WriteUInt32((uint)PrefabCount);
		writer.WriteUInt16(IdOffset);

		foreach (var prefab in _groups.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw(false)))
		{
			prefab.Save(writer);
		}
	}

	public void AddGroup(PrefabGroup group)
	{
		if (group.Id != PrefabCount + IdOffset)
		{
			ThrowHelper.ThrowArgumentException($"{group.Id} must be equal to {nameof(PrefabCount)} + {nameof(IdOffset)}.", nameof(group));
		}

		_groups.Add(group.Id, group);
		_prefabs.AddRange(group.Values);
	}

	public bool RemoveGroup(ushort id)
	{
		if (!_groups.Remove(id, out var group))
		{
			return false;
		}

		_prefabs.RemoveRange(id, group.Count);

		if (IsLastGroup(group))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)group.Count);

		return true;
	}

	public void AddPrefabToGroup(ushort id, Prefab prefab)
	{
		var group = _groups[id];
		group.Add(prefab.PosInGroup, prefab);

		if (IsLastGroup(group))
		{
			_prefabs.Add(prefab);
			return;
		}

		int prefabId = group.Id + group.Count;
		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);
	}

	public bool TryAddPrefabToGroup(ushort id, Prefab prefab)
	{
		if (!_groups.TryGetValue(id, out var group) || !group.TryAdd(prefab))
		{
			return false;
		}

		if (IsLastGroup(group))
		{
			_prefabs.Add(prefab);
			return true;
		}

		int prefabId = group.Id + group.Count;
		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);

		return true;
	}

	public bool RemovePrefabFromGroup(ushort id, byte3 posInGroup)
	{
		var group = _groups[id];
		if (!group.Remove(posInGroup))
		{
			return false;
		}

		int index = group.IndexOf(posInGroup);
		ushort prefabId = (ushort)(id + index);

		_prefabs.RemoveAt(PrefabCount - 1);
		RemovePrefabId(prefabId);

		if (prefabId == PrefabCount + IdOffset - 1)
		{
			return true;
		}

		DecreaseAfter(prefabId, 1);

		return true;
	}

	/// <summary>
	/// Creates a copy of this <see cref="PrefabList"/>.
	/// </summary>
	/// <param name="deepCopy">If deep copy should be performed.</param>
	/// <returns>A copy of this <see cref="PrefabList"/>.</returns>
	public PrefabList Clone(bool deepCopy)
		=> new PrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PrefabList(this, true);

	private static IEnumerable<Prefab> PrefabsFromGroups(IEnumerable<KeyValuePair<ushort, PrefabGroup>> groups)
		=> groups.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

	private bool IsLastGroup(PrefabGroup group)
		=> group.Id + group.Count == PrefabCount + IdOffset;

	private void RemovePrefabId(ushort id)
	{
		foreach (var group in _groups.Values)
		{
			ushort[] array = group.Blocks.Array.Array;

			for (int z = 0; z < group.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < group.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < group.Blocks.Size.X; x++)
					{
						int i = group.Blocks.GetBlockUnchecked(new int3(x, y, z));

						if (array[i] == id)
						{
							array[i] = 0;
						}
					}
				}
			}
		}
	}

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _prefabs.Count; i++)
		{
			Prefab prefab = this[i];

			if (prefab.GroupId >= index)
			{
				prefab.GroupId += amount;
			}
		}

		List<ushort> groupsToChangeId = [];

		foreach (var (id, group) in _groups)
		{
			if (id >= index)
			{
				groupsToChangeId.Add(id);
			}

			ushort[] array = group.Blocks.Array.Array;

			for (int z = 0; z < group.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < group.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < group.Blocks.Size.X; x++)
					{
						int i = group.Blocks.GetBlockUnchecked(new int3(x, y, z));

						if (array[i] >= index)
						{
							array[i] += amount;
						}
					}
				}
			}
		}

		foreach (ushort id in groupsToChangeId.OrderByDescending(item => item))
		{
			bool removed = _groups.Remove(id, out var group);

			Debug.Assert(removed, "Group should have been removed.");
			Debug.Assert(group is not null, $"{group} shouldn't be null.");

			_groups[(ushort)(id + amount)] = group;
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _prefabs.Count; i++)
		{
			Prefab prefab = this[i];

			if (prefab.GroupId >= index)
			{
				prefab.GroupId -= amount;
			}
		}

		List<ushort> groupsToChangeId = [];

		foreach (var (id, group) in _groups)
		{
			if (id >= index)
			{
				groupsToChangeId.Add(id);
			}

			ushort[] array = group.Blocks.Array.Array;

			for (int z = 0; z < group.Blocks.Size.Z; z++)
			{
				for (int y = 0; y < group.Blocks.Size.Y; y++)
				{
					for (int x = 0; x < group.Blocks.Size.X; x++)
					{
						int i = group.Blocks.GetBlockUnchecked(new int3(x, y, z));

						if (array[i] >= index)
						{
							array[i] -= amount;
						}
					}
				}
			}
		}

		foreach (ushort id in groupsToChangeId.OrderBy(item => item))
		{
			bool removed = _groups.Remove(id, out var group);

			Debug.Assert(removed, "Group should have been removed.");
			Debug.Assert(group is not null, $"{group} shouldn't be null.");

			_groups[(ushort)(id - amount)] = group;
		}
	}
}

// <copyright file="PrefabList.cs" company="BitcoderCZ">
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

	public PrefabList()
	{
		_groups = [];
		_prefabs = [];
	}

	public PrefabList(int groupCapacity, int prefabCapacity)
	{
		_groups = new(groupCapacity);
		_prefabs = new(prefabCapacity);
	}

	public PrefabList(IEnumerable<PrefabGroup> collection)
	{
		if (collection is null)
		{
			ThrowArgumentNullException(nameof(collection));
		}

		_groups = collection.ToDictionary(group => group.Id);
		ValidateGroups(_groups.Values, nameof(collection)); // validate using _groups.Values to avoid iterating over collection multiple times

		_prefabs = [.. PrefabsFromGroups(_groups)];

		IdOffset = _groups.Min(item => item.Key);
	}

	public PrefabList(PrefabList list, bool deepCopy)
	{
		ThrowIfNull(list, nameof(list));

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

	public IEnumerable<PrefabGroup> Groups => _groups.Values;

	public IEnumerable<Prefab> Prefabs => _prefabs;

	public static PrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowArgumentNullException(nameof(reader));
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
				} while (i < count && rawPrefabs[i].GroupId == groupId);

				ushort id = (ushort)(startIndex + idOffset);
				groups.Add(id, PrefabGroup.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex), ushort.MaxValue, 0, false));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + idOffset);
				groups.Add(id, PrefabGroup.FromRaw(id, [rawPrefabs[i]], ushort.MaxValue, 0, false));
			}
		}

		return new PrefabList(groups)
		{
			IdOffset = idOffset,
		};
	}

	public void Save(FcBinaryWriter writer)
	{
		if (writer is null)
		{
			ThrowArgumentNullException(nameof(writer));
		}

		writer.WriteUInt32((uint)PrefabCount);
		writer.WriteUInt16(IdOffset);

		foreach (var prefab in _groups.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw(false)))
		{
			prefab.Save(writer);
		}
	}

	public PrefabGroup GetGroup(ushort id)
		=> _groups[id];

	public bool TryGetGroup(ushort id, [MaybeNullWhen(false)] out PrefabGroup group)
		=> _groups.TryGetValue(id, out group);

	public Prefab GetPrefab(ushort id)
		=> _prefabs[id - IdOffset];

	public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out Prefab prefab)
	{
		id -= IdOffset;

		// can skip (id >= 0) because id is unsigned
		if (id < _prefabs.Count)
		{
			prefab = _prefabs[id];
			return true;
		}
		else
		{
			prefab = null;
			return false;
		}
	}

	public void AddGroup(PrefabGroup group)
	{
		if (group.Id != PrefabCount + IdOffset)
		{
			ThrowArgumentException($"{group.Id} must be equal to {nameof(PrefabCount)} + {nameof(IdOffset)}.", nameof(group));
		}

		_groups.Add(group.Id, group);
		_prefabs.AddRange(group.Values);
	}

	public void InsertGroup(PrefabGroup group)
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
		_prefabs.InsertRange(group.Id - IdOffset, group.Values);
	}

	public bool RemoveGroup(ushort id)
	{
		if (!_groups.Remove(id, out var group))
		{
			return false;
		}

		_prefabs.RemoveRange(id - IdOffset, group.Count);

		for (int i = 0; i < group.Count; i++)
		{
			RemoveIdFromBlocks((ushort)(id + i));
		}

		if (WillBeLastGroup(group))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)group.Count);

		return true;
	}

	public void AddPrefabToGroup(ushort id, Prefab prefab, bool overwriteBlocks)
	{
		var group = _groups[id];

		if (!overwriteBlocks && !CanAddIdToGroup(id, prefab.PosInGroup))
		{
			throw new InvalidOperationException($"Cannot add prefab because it's position is obstructed and {nameof(overwriteBlocks)} is false.");
		}

		ushort prefabId = (ushort)(group.Id + group.Count);

		if (IsLastGroup(group))
		{
			group.Add(prefab.PosInGroup, prefab);
			_prefabs.Add(prefab);
			AddIdToGroup(id, prefab.PosInGroup, prefabId);
			return;
		}

		group.Add(prefab.PosInGroup, prefab);

		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);
		AddIdToGroup(id, prefab.PosInGroup, prefabId);
	}

	public bool TryAddPrefabToGroup(ushort id, Prefab prefab, bool overwriteBlocks)
	{
		if (!_groups.TryGetValue(id, out var group))
		{
			return false;
		}

		if (!overwriteBlocks && !CanAddIdToGroup(id, prefab.PosInGroup))
		{
			return false;
		}

		if (!group.TryAdd(prefab))
		{
			return false;
		}

		ushort prefabId = (ushort)(group.Id + group.Count - 1);

		if (IsLastGroup(group))
		{
			_prefabs.Add(prefab);
			AddIdToGroup(id, prefab.PosInGroup, prefabId);
			return true;
		}

		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);
		AddIdToGroup(id, prefab.PosInGroup, prefabId);

		return true;
	}

	public bool RemovePrefabFromGroup(ushort id, byte3 posInGroup)
	{
		var group = _groups[id];

		int prefabIndex = group.IndexOf(posInGroup);
		if (!group.Remove(posInGroup))
		{
			return false;
		}

		ushort prefabId = (ushort)(id + prefabIndex);

		_prefabs.RemoveAt(prefabId - IdOffset);
		RemoveIdFromBlocks(prefabId);

		if (prefabId == PrefabCount + IdOffset - 1)
		{
			return true;
		}

		DecreaseAfter(prefabId, 1);

		return true;
	}

	public PrefabList Clone(bool deepCopy)
		=> new PrefabList(this, deepCopy);

	/// <inheritdoc/>
	object ICloneable.Clone()
		=> new PrefabList(this, true);

	private static void ValidateGroups(IEnumerable<PrefabGroup> groups, string paramName)
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

	private static IEnumerable<Prefab> PrefabsFromGroups(IEnumerable<KeyValuePair<ushort, PrefabGroup>> groups)
		=> groups.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

	private bool IsLastGroup(PrefabGroup group)
		=> group.Id + group.Count >= PrefabCount + IdOffset;

	private bool WillBeLastGroup(PrefabGroup group)
		=> group.Id >= PrefabCount + IdOffset;

	private void RemoveIdFromBlocks(ushort id)
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
						int i = group.Blocks.Index(new int3(x, y, z));

						if (array[i] == id)
						{
							array[i] = 0;
						}
					}
				}
			}
		}
	}

	private bool CanAddIdToGroup(ushort groupId, int3 offset)
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
						int3 pos = new int3(x, y, z);
						int i = group.Blocks.Index(pos);

						if (array[i] == groupId)
						{
							pos += offset;
							if (group.Blocks.InBounds(pos) && group.Blocks.GetBlockUnchecked(pos) != 0)
							{
								return false;
							}
						}
					}
				}
			}
		}

		return true;
	}

	private void AddIdToGroup(ushort groupId, int3 offset, ushort id)
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
						int3 pos = new int3(x, y, z);
						int i = group.Blocks.Index(pos);

						if (array[i] == groupId)
						{
							group.Blocks.SetBlock(pos + offset, id);
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
			Prefab prefab = _prefabs[i];

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
						int i = group.Blocks.Index(new int3(x, y, z));

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

			ushort newId = (ushort)(id + amount);
			group.Id = newId;
			_groups[newId] = group;
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _prefabs.Count; i++)
		{
			Prefab prefab = _prefabs[i];

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
						int i = group.Blocks.Index(new int3(x, y, z));

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

			ushort newId = (ushort)(id - amount);
			group.Id = newId;
			_groups[newId] = group;
		}
	}
}

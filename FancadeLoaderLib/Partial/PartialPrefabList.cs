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
/// Group ids are automatically changed when prefabs are inserter/removed.
/// <para>Allows for saving/loading.</para>
/// </remarks>
public partial class PartialPrefabList : ICloneable
{
	/// <summary>
	/// The id offset of this list, <see cref="RawGame.CurrentNumbStockPrefabs"/> by default.
	/// </summary>
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	internal readonly Dictionary<ushort, PartialPrefabGroup> _groups;
	internal readonly List<PartialPrefab> _prefabs;

	public PartialPrefabList()
	{
		_groups = [];
		_prefabs = [];
	}

	public PartialPrefabList(int groupCapacity, int prefabCapacity)
	{
		_groups = new(groupCapacity);
		_prefabs = new(prefabCapacity);
	}

	public PartialPrefabList(IEnumerable<PartialPrefabGroup> collection)
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

	public PartialPrefabList(PartialPrefabList list, bool deepCopy)
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

	private PartialPrefabList(Dictionary<ushort, PartialPrefabGroup> dict)
	{
		_groups = dict;
		_prefabs = [.. PrefabsFromGroups(_groups)];
	}

	public int GroupCount => _groups.Count;

	public int PrefabCount => _prefabs.Count;

	public IEnumerable<PartialPrefabGroup> Groups => _groups.Values;

	public IEnumerable<PartialPrefab> Prefabs => _prefabs;

	public static PartialPrefabList Load(FcBinaryReader reader)
	{
		if (reader is null)
		{
			ThrowArgumentNullException(nameof(reader));
		}

		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		OldPartialPrefab[] rawPrefabs = new OldPartialPrefab[count];

		for (int i = 0; i < count; i++)
		{
			rawPrefabs[i] = OldPartialPrefab.Load(reader);
		}

		Dictionary<ushort, PartialPrefabGroup> groups = [];

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
				groups.Add(id, PartialPrefabGroup.FromRaw(id, rawPrefabs.Skip(startIndex).Take(i - startIndex)));

				i--; // incremented at the end of the loop
			}
			else
			{
				ushort id = (ushort)(i + idOffset);
				groups.Add(id, PartialPrefabGroup.FromRaw(id, [rawPrefabs[i]]));
			}
		}

		return new PartialPrefabList(groups)
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

		foreach (var prefab in _groups.OrderBy(item => item.Key).SelectMany(item => item.Value.ToRaw()))
		{
			prefab.Save(writer);
		}
	}

	public PartialPrefabGroup GetGroup(ushort id)
		=> _groups[id];

	public bool TryGetGroup(ushort id, [MaybeNullWhen(false)] out PartialPrefabGroup group)
		=> _groups.TryGetValue(id, out group);

	public PartialPrefab GetPrefab(ushort id)
		=> _prefabs[id - IdOffset];

	public bool TryGetPrefab(ushort id, [MaybeNullWhen(false)] out PartialPrefab prefab)
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

	public void AddGroup(PartialPrefabGroup group)
	{
		if (group.Id != PrefabCount + IdOffset)
		{
			ThrowArgumentException($"{group.Id} must be equal to {nameof(PrefabCount)} + {nameof(IdOffset)}.", nameof(group));
		}

		_groups.Add(group.Id, group);
		_prefabs.AddRange(group.Values);
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
		_prefabs.InsertRange(group.Id - IdOffset, group.Values);
	}

	public bool RemoveGroup(ushort id)
	{
		if (!_groups.Remove(id, out var group))
		{
			return false;
		}

		_prefabs.RemoveRange(id - IdOffset, group.Count);

		if (WillBeLastGroup(group))
		{
			return true;
		}

		DecreaseAfter(id, (ushort)group.Count);

		return true;
	}

	public void AddPrefabToGroup(ushort id, PartialPrefab prefab)
	{
		var group = _groups[id];

		ushort prefabId = (ushort)(group.Id + group.Count);

		if (IsLastGroup(group))
		{
			group.Add(prefab.PosInGroup, prefab);
			_prefabs.Add(prefab);
			return;
		}

		group.Add(prefab.PosInGroup, prefab);

		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);
	}

	public bool TryAddPrefabToGroup(ushort id, PartialPrefab prefab)
	{
		if (!_groups.TryGetValue(id, out var group))
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
			return true;
		}

		IncreaseAfter(prefabId, 1);
		_prefabs.Insert(prefabId, prefab);

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

		if (prefabId == PrefabCount + IdOffset - 1)
		{
			return true;
		}

		DecreaseAfter(prefabId, 1);

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

	private static IEnumerable<PartialPrefab> PrefabsFromGroups(IEnumerable<KeyValuePair<ushort, PartialPrefabGroup>> groups)
		=> groups.OrderBy(item => item.Key).SelectMany(item => item.Value.Values);

	private bool IsLastGroup(PartialPrefabGroup group)
		=> group.Id + group.Count >= PrefabCount + IdOffset;

	private bool WillBeLastGroup(PartialPrefabGroup group)
		=> group.Id >= PrefabCount + IdOffset;

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _prefabs.Count; i++)
		{
			PartialPrefab prefab = _prefabs[i];

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
			PartialPrefab prefab = _prefabs[i];

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

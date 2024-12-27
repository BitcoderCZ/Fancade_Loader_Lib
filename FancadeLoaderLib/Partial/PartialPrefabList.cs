// <copyright file="PartialPrefabList.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Raw;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FancadeLoaderLib.Partial;

/// <summary>
/// <see cref="PrefabList"/> for <see cref="PartialPrefab"/>.
/// </summary>
public class PartialPrefabList : IList<PartialPrefab>, ICloneable
{
	public ushort IdOffset = RawGame.CurrentNumbStockPrefabs;

	private readonly List<PartialPrefab> _list;

	public PartialPrefabList()
	{
		_list = [];
	}

	public PartialPrefabList(int capacity)
	{
		_list = new List<PartialPrefab>(capacity);
	}

	public PartialPrefabList(IEnumerable<PartialPrefab> collection)
	{
		_list = [.. collection];
	}

	public PartialPrefabList(PartialPrefabList list, bool deepCopy)
	{
		_list = deepCopy
			? [.. list.Select(prefab => prefab.Clone())]
			: [.. list];
	}

	private PartialPrefabList(List<PartialPrefab> list)
	{
		_list = list;
	}

	public int Count => _list.Count;

	public bool IsReadOnly => false;

	public PartialPrefab this[int index]
	{
		get => _list[index];
		set => _list[index] = value;
	}

	public static PartialPrefabList Load(FcBinaryReader reader)
	{
		uint count = reader.ReadUInt32();
		ushort idOffset = reader.ReadUInt16();

		List<PartialPrefab> list = new List<PartialPrefab>((int)count);

		for (int i = 0; i < count; i++)
		{
			list.Add(PartialPrefab.Load(reader));
		}

		return new PartialPrefabList(list)
		{
			IdOffset = idOffset,
		};
	}

	public void Save(FcBinaryWriter writer)
	{
		writer.WriteUInt32((uint)Count);
		writer.WriteUInt16(IdOffset);

		for (int i = 0; i < Count; i++)
		{
			this[i].Save(writer);
		}
	}

	public void Add(PartialPrefab item)
		=> _list.Add(item);

	public void AddRange(IEnumerable<PartialPrefab> collection)
		=> _list.AddRange(collection);

	public void AddGroup(PartialPrefabGroup group)
	{
		group.Id = (ushort)_list.Count;
		_list.AddRange(group.EnumerateInIdOrder());
	}

	public void Clear()
		=> _list.Clear();

	public bool Contains(PartialPrefab item)
		=> _list.Contains(item);

	public void CopyTo(PartialPrefab[] array, int arrayIndex)
		=> _list.CopyTo(array, arrayIndex);

	public void CopyTo(PartialPrefab[] array)
		=> _list.CopyTo(array);

	public void CopyTo(int index, PartialPrefab[] array, int arrayIndex, int count)
		=> _list.CopyTo(index, array, arrayIndex, count);

	public bool Exists(Predicate<PartialPrefab> match)
		=> _list.Exists(match);

	public PartialPrefab Find(Predicate<PartialPrefab> match)
		=> _list.Find(match);

	public List<PartialPrefab> FindAll(Predicate<PartialPrefab> match)
		=> _list.FindAll(match);

	public int FindIndex(int startIndex, int count, Predicate<PartialPrefab> match)
		=> _list.FindIndex(startIndex, count, match);

	public int FindIndex(int startIndex, Predicate<PartialPrefab> match)
		=> _list.FindIndex(startIndex, match);

	public int FindIndex(Predicate<PartialPrefab> match)
		=> _list.FindIndex(match);

	public PartialPrefab FindLast(Predicate<PartialPrefab> match)
		=> _list.FindLast(match);

	public int FindLastIndex(int startIndex, int count, Predicate<PartialPrefab> match)
		=> _list.FindLastIndex(startIndex, count, match);

	public int FindLastIndex(int startIndex, Predicate<PartialPrefab> match)
		=> _list.FindLastIndex(startIndex, match);

	public int FindLastIndex(Predicate<PartialPrefab> match)
		=> _list.FindLastIndex(match);

	public void Insert(int index, PartialPrefab item)
	{
		IncreaseAfter(index, 1);
		_list.Insert(index, item);
	}

	public void InsertRange(int index, IEnumerable<PartialPrefab> collection)
	{
		int count = collection.Count();
		IncreaseAfter(index, (ushort)count);
		_list.InsertRange(index, collection);
	}

	public void InsertGroup(int index, PartialPrefabGroup group)
	{
		IncreaseAfter(index, (ushort)group.Count);
		group.Id = (ushort)index;
		_list.InsertRange(index, group.EnumerateInIdOrder());
	}

	public int LastIndexOf(PartialPrefab item)
		=> _list.LastIndexOf(item);

	public int LastIndexOf(PartialPrefab item, int index)
		=> _list.LastIndexOf(item, index);

	public int LastIndexOf(PartialPrefab item, int index, int count)
		=> _list.LastIndexOf(item, index, count);

	public int IndexOf(PartialPrefab item)
		=> _list.IndexOf(item);

	public bool Remove(PartialPrefab item)
	{
		int index = _list.IndexOf(item);
		if (index < 0)
		{
			return false;
		}
		else
		{
			RemoveAt(index);
			return true;
		}
	}

	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
		DecreaseAfter(index - 1, 1);
	}

	public void RemoveRange(int index, int count)
	{
		_list.RemoveRange(index, count);
		DecreaseAfter(index - 1, (ushort)count);
	}

	public bool TrueForAll(Predicate<PartialPrefab> match)
		=> _list.TrueForAll(match);

	public PartialPrefab[] ToArray()
		=> [.. _list];

	public IEnumerator<PartialPrefab> GetEnumerator()
		=> _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _list.GetEnumerator();

	public PartialPrefabList Clone(bool deepCopy)
		=> new PartialPrefabList(this, deepCopy);

	object ICloneable.Clone()
		=> new PartialPrefabList(this, true);

	private void IncreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			PartialPrefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId += amount;
			}
		}
	}

	private void DecreaseAfter(int index, ushort amount)
	{
		index += IdOffset;

		for (int i = 0; i < _list.Count; i++)
		{
			PartialPrefab prefab = this[i];

			if (prefab.IsInGroup && prefab.GroupId >= index)
			{
				prefab.GroupId -= amount;
			}
		}
	}
}
